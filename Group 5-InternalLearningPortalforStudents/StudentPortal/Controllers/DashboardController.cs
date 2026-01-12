using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    public class DashboardController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly StudentPortalContext _context;
        private readonly SignInManager<User> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public DashboardController(UserManager<User> userManager, StudentPortalContext context, SignInManager<User> signInManager, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction(nameof(AdminIndex));
            }
            if (User.IsInRole("Lecturer"))
            {
                return RedirectToAction(nameof(LecturerIndex));
            }

            if (User.IsInRole("Student"))
            {
                return RedirectToAction("StudentIndex", "Dashboard");
            }
            return Redirect("/Identity/Account/Login");
        }

        [Authorize]
        public async Task<ActionResult> StudentIndex(DateTime? date)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                await _signInManager.SignOutAsync();
                return Redirect("/Account/Login");
            }

            ViewData["FullName"] = currentUser.FullName;
            ViewData["Email"] = currentUser.Email;
            ViewData["Phone"] = currentUser.PhoneNumber;

            var student = await _context.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null)
            {
                return View("Error");
            }
            ViewData["StudentCode"] = student.StudentCode;
            ViewData["Deparment"] = student.Department.DepartmentName;

            // --- PHẦN 1: THỜI KHÓA BIỂU (ĐÃ SỬA) ---
            DateTime selectedDate = date ?? DateTime.Today;
            ViewData["selectedDate"] = selectedDate;

            var studentSchedule = await _context.ScheduleItems
                .Include(s => s.CourseSection).ThenInclude(cs => cs.Course)
                .Include(s => s.CourseSection).ThenInclude(cs => cs.Enrollments)
                .Include(s => s.CourseSection).ThenInclude(cs => cs.Semester) // [MỚI] Include thêm Semester
                .Where(s => s.CourseSection.Enrollments.Any(e => e.StudentId == student.StudentId)
                            && s.ScheduleDate.Date == selectedDate.Date
                            && s.CourseSection.Semester.IsActive == true) // [MỚI] Chỉ lấy lịch của kỳ đang Active
                .OrderBy(s => s.CourseSection.Sessions)
                .ToListAsync();

            ViewData["ListSchedule"] = studentSchedule;

            // --- PHẦN 2: TÍNH ĐIỂM (ĐÃ GIA CỐ AN TOÀN) ---
            var listEnrollment = await _context.Enrollments
                .Include(e => e.CourseSection).ThenInclude(cs => cs.Course)
                .Where(e => e.StudentId == student.StudentId && e.Status == EnrollmentStatus.Finished)
                .ToListAsync();

            // Tính tổng tín chỉ (dùng int? ?? 0 để an toàn)
            int totalCredits = listEnrollment.Sum(e => e.CourseSection?.Course?.CourseCredit ?? 0);

            var listScores = await _context.Scores
                .Where(s => s.StudentId == student.StudentId)
                .ToListAsync();

            double TotalWeightScore = 0;

            foreach (var enrollment in listEnrollment)
            {
                // Tìm điểm tương ứng với môn học
                var score = listScores.FirstOrDefault(s => s.CourseSectionId == enrollment.CourseSectionId);

                // [QUAN TRỌNG] Kiểm tra null trước khi cộng. 
                // Nếu enrollment là Finished mà chưa vào điểm (score == null) thì bỏ qua để tránh lỗi Crash
                if (score != null && enrollment.CourseSection?.Course != null)
                {
                    TotalWeightScore += score.FinalScore * enrollment.CourseSection.Course.CourseCredit;
                }
            }

            double totalGPA = 0;

            if (totalCredits > 0)
            {
                totalGPA = TotalWeightScore / totalCredits;
                totalGPA = Math.Round(totalGPA, 2);
            }

            if(totalGPA == 0)
            {
                ViewData["TotalScore"] = "";
            }
            else
            {
                ViewData["TotalScore"] = totalGPA;
            }
     
            ViewData["TotalCredits"] = totalCredits;

            return View();
        }

        [Authorize(Roles = "Lecturer")]
        public IActionResult LecturerIndex(DateTime? date)
        {
            var currentUser = _userManager.GetUserAsync(User).Result;
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var lecturer = _context.Lecturers
                .Include(l => l.User)
                .Include(l => l.Faculty)
                .FirstOrDefault(l => l.UserId == currentUser.Id);

            if (lecturer == null)
            {
                return View("Error");
            }

            ViewData["Name"] = lecturer.User.FullName;

            DateTime selectedDate = date ?? DateTime.Today;
            ViewData["selectedDate"] = selectedDate;

            var schedule = _context.ScheduleItems
                .Include(si => si.CourseSection).ThenInclude(cs => cs.Lecturer)
                .Include(si => si.CourseSection).ThenInclude(cs => cs.Course) 
                .Include(si => si.CourseSection).ThenInclude(cs => cs.Semester) 
                .Where(si => si.CourseSection.LecturerId == lecturer.LecturerId
                          && si.ScheduleDate.Date == selectedDate.Date
                          && si.CourseSection.Semester.IsActive == true) 
                .ToList();

            ViewData["ListSchedule"] = schedule;

            var teachingList = _context.CoursesSections
                .Include(cs => cs.Course)
                .Include(cs => cs.Lecturer)
                .Include(cs => cs.Semester)
                .Where(cs => cs.LecturerId == lecturer.LecturerId
                        && cs.Semester.IsActive)
                .ToList();

            if (teachingList == null || !teachingList.Any())
            {
                ViewData["ErrorMessage"] = "Hiện tại không có học kỳ nào đang hoạt động hoặc bạn chưa được phân công lớp dạy.";

                ViewData["teachingList"] = new List<CourseSection>();

                ViewData["CurrentSemester"] = "";
            }
            else
            {
                ViewData["teachingList"] = teachingList;
                ViewData["CurrentSemester"] = teachingList.FirstOrDefault()?.Semester?.SemesterName;
            }

            return View();
        }

        [Authorize(Roles = "Student")]
        public async Task<ActionResult> StudentProfile()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return RedirectToAction("Login", "Account");

                var currentStudent = await _context.Students
                    .Include(s => s.Department)
                    .ThenInclude(d => d.Faculty)
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

                if (currentStudent == null) return View("Error");

                ViewData["Name"] = currentStudent.User.FullName;
                ViewData["Department"] = currentStudent.Department.DepartmentName;
                ViewData["City"] = currentStudent.User.City ?? "Chưa có thông tin ";
                ViewData["Email"] = currentStudent.User.Email;
                ViewData["Code"] = currentStudent.StudentCode;
                ViewData["PhoneNumber"] = currentStudent.User.PhoneNumber ?? "Chưa có thông tin";
                ViewData["Faculty"] = currentStudent.Department.Faculty.FacultyName;

                var listEnrollment = await _context.Enrollments
                    .Include(e => e.CourseSection)
                    .ThenInclude(cs => cs.Course)
                    .Where(e => e.StudentId == currentStudent.StudentId)
                    .ToListAsync();

                int totalCredits = listEnrollment.Sum(e => e.CourseSection?.Course?.CourseCredit ?? 0);

                var listScores = await _context.Scores
                    .Where(s => s.StudentId == currentStudent.StudentId)
                    .ToListAsync();

                double TotalWeightScore = 0;

                foreach (var enrollment in listEnrollment)
                {
                    var score = listScores.FirstOrDefault(s => s.CourseSectionId == enrollment.CourseSectionId);
                    TotalWeightScore += score.FinalScore * enrollment.CourseSection.Course.CourseCredit;
                }

                double totalGPA = 0;

                if(totalCredits > 0)
                {
                    totalGPA = TotalWeightScore / totalCredits;
                    totalGPA = Math.Round(totalGPA, 2);
                }

                if (totalGPA == 0)
                {
                    ViewData["TotalScore"] = "";
                }
                else
                {
                    ViewData["TotalScore"] = totalGPA;
                }
                ViewData["TotalCredits"] = totalCredits;
                ViewData["CourseList"] = listEnrollment;
                return View(currentStudent);
            }
            catch (Exception)
            { return View("Error"); }
        }

        [Authorize(Roles = "Lecturer")]
        public async Task<ActionResult> LecturerProfile()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return RedirectToAction("Login", "Account");

                var lecturer = await _context.Lecturers
                    .Include(l => l.Faculty)
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.UserId == currentUser.Id);

                if (lecturer == null) return View("Error");

                // --- Giữ nguyên thông tin cá nhân ---
                ViewData["Name"] = lecturer.User.FullName;
                ViewData["Faculty"] = lecturer.Faculty?.FacultyName ?? "Khoa";
                ViewData["City"] = lecturer.User.City ?? "Chưa có thông tin";
                ViewData["Email"] = lecturer.User.Email;
                ViewData["PhoneNumber"] = lecturer.User.PhoneNumber ?? "Chưa có thông tin";

                // --- XỬ LÝ THỐNG KÊ MỚI ---

                // 1. Lấy tất cả lớp mà giảng viên dạy (kèm thông tin môn học)
                var allSections = await _context.CoursesSections
                    .Include(cs => cs.Course)
                    .Where(cs => cs.LecturerId == lecturer.LecturerId)
                    .ToListAsync();

                // 2. Tính tổng số lớp (Số dòng trong bảng phân công)
                int totalClassCount = allSections.Count;

                // 3. Group theo Môn học để đếm số lần dạy từng môn
                // Kết quả là một List chứa các cặp (Môn học, Số lượng)
                var courseStats = allSections
                    .GroupBy(cs => cs.Course)
                    .Select(g => (Course: g.Key, Count: g.Count()))
                    .ToList();

                // 4. Tính tổng số môn (Số lượng nhóm sau khi group)
                int totalSubjectCount = courseStats.Count;

                // Truyền dữ liệu sang View
                ViewData["TotalClass"] = totalClassCount;
                ViewData["TotalSubjects"] = totalSubjectCount; // Số môn
                ViewData["CourseStats"] = courseStats;         // Danh sách (Môn, Số lần)

                return View(lecturer);
            }
            catch (Exception)
            { return View("Error"); }
        }

        [HttpGet]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> EditLecturerProfile()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // Tìm Lecturer dựa trên UserId, kèm theo thông tin User
            var currentLecturer = await _context.Lecturers
                .Include(l => l.User)
                .Include(l => l.Faculty)
                .FirstOrDefaultAsync(l => l.UserId == currentUser.Id);

            if (currentLecturer == null) return NotFound();

            // Trả về PartialView riêng cho Giảng viên
            return PartialView("_EditLecturerProfileModal", currentLecturer);
        }

        // 2. POST: Cập nhật thông tin Giảng viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> UpdateLecturerProfile(Lecturer modelInput, IFormFile? avatarFile)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var lecturerInDb = await _context.Lecturers
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.UserId == currentUser.Id);

            if (lecturerInDb != null)
            {
                lecturerInDb.User.PhoneNumber = modelInput.User.PhoneNumber;
                lecturerInDb.User.City = modelInput.User.City;
                lecturerInDb.User.Address = modelInput.User.Address;

                if (avatarFile != null && avatarFile.Length > 0)
                {
                    var fileName = $"avatar_{lecturerInDb.UserId}_{Guid.NewGuid()}{Path.GetExtension(avatarFile.FileName)}";

                    var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "avatars");

                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await avatarFile.CopyToAsync(stream);
                    }

                    if (!string.IsNullOrEmpty(lecturerInDb.User.ImagePath))
                    {
                        var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, lecturerInDb.User.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    lecturerInDb.User.ImagePath = "/images/avatars/" + fileName;
                }

                _context.Update(lecturerInDb);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }

            ViewData["Avatar"] = lecturerInDb.User.ImagePath ?? "/images/default-avatar.png"; 
            return PartialView("_EditLecturerProfileModal", modelInput);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")] 
        public async Task<IActionResult> UpdateStudentProfile(Student modelInput, IFormFile? avatarFile)
        {
            // 1. Lấy User hiện tại
            var currentUser = await _userManager.GetUserAsync(User);

            var studentInDb = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (studentInDb != null)
            {
                studentInDb.User.PhoneNumber = modelInput.User.PhoneNumber;
                studentInDb.User.City = modelInput.User.City;
                studentInDb.User.Address = modelInput.User.Address;
                studentInDb.User.Country = modelInput.User.Country;
                studentInDb.User.DateOfBirth = modelInput.User.DateOfBirth;

                if (avatarFile != null && avatarFile.Length > 0)
                {
                    var fileName = $"avatar_{studentInDb.UserId}_{Guid.NewGuid()}{Path.GetExtension(avatarFile.FileName)}";

                    var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "avatars");

                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await avatarFile.CopyToAsync(stream);
                    }

                    if (!string.IsNullOrEmpty(studentInDb.User.ImagePath))
                    {
                        var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, studentInDb.User.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    studentInDb.User.ImagePath = "/images/avatars/" + fileName;
                }

                _context.Update(studentInDb);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }

            ViewData["Avatar"] = studentInDb?.User.ImagePath ?? "/images/default-avatar.png";
            return PartialView("_EditProfileModal", modelInput);
        }


        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // Tìm Student dựa trên UserId
            var currentStudent = await _context.Students
                .Include(s => s.User) 
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (currentStudent == null) return NotFound();

            return PartialView("_EditProfileModal", currentStudent);
        }

        // 2. POST: Cập nhật thông tin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfileFast(Student modelInput)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var studentInDb = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (studentInDb != null)
            {
                // Kiểm tra xem View có gửi thông tin User lên không
                if (modelInput.User != null)
                {
                    // Đảm bảo DB có User để gán
                    if (studentInDb.User == null) studentInDb.User = currentUser;

                    // --- CHỈ CẬP NHẬT NHỮNG GÌ VIEW GỬI LÊN ---

                    // 1. Cập nhật Số điện thoại
                    if (!string.IsNullOrEmpty(modelInput.User.PhoneNumber))
                    {
                        studentInDb.User.PhoneNumber = modelInput.User.PhoneNumber;
                    }

                    // 2. Cập nhật Họ tên (Nếu View cho phép sửa)
                    if (!string.IsNullOrEmpty(modelInput.User.FullName))
                    {
                        studentInDb.User.FullName = modelInput.User.FullName;
                    }

                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật hồ sơ thành công!";
                return RedirectToAction("StudentIndex");
            }

            // Nếu lỗi thì quay lại trang cũ
            return RedirectToAction("StudentIndex");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // 1. THỐNG KÊ TỔNG QUAN (Stats Cards)
            ViewData["TotalStudents"] = await _context.Students.CountAsync();

            // Đếm giảng viên (trừ user system nếu có)
            ViewData["TotalLecturers"] = await _context.Lecturers.CountAsync(); 

            // Đếm số lớp học phần đang mở (Kiểm tra lại tên bảng trong DBContext của bạn là CourseSections hay CoursesSections nhé)
            // Ở đây tôi dùng theo code cũ của bạn là CoursesSections
            ViewData["TotalCourses"] = await _context.CoursesSections.CountAsync();

            // 2. HỌC KỲ HIỆN TẠI
            var today = DateTime.Now;
            var currentSemester = await _context.Semesters
                .Where(s => s.IsActive)
                .FirstOrDefaultAsync();

            ViewData["CurrentSemesterName"] = currentSemester != null ? currentSemester.SemesterName : "Chưa thiết lập";

            // 3. (Tùy chọn) Lấy 5 thông báo mới nhất để hiển thị 
            var recentAnnouncements = await _context.Announcements
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedDate)
                .Take(5)
                .ToListAsync();
            ViewData["RecentAnnouncements"] = recentAnnouncements;

            return View(); // Trả về Views/Dashboard/AdminIndex.cshtml
        }

    }
}