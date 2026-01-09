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

            var student = await _context.Students.Include(s => s.Department).FirstOrDefaultAsync(s => s.UserId == currentUser.Id);
            if (student == null)
            {
                return View("Error");
            }
            ViewData["StudentCode"] = student.StudentCode;
            ViewData["Deparment"] = student.Department.DepartmentName;

            //Thời khóa biểu ở trang index
            DateTime selectedDate = date ?? DateTime.Today;
            ViewData["selectedDate"] = selectedDate;

            var studentSchedule = await _context.ScheduleItems
                .Include(s => s.CourseSection).ThenInclude(cs => cs.Course)
                .Include(s => s.CourseSection).ThenInclude(cs => cs.Enrollments)
                .Where(s => s.CourseSection.Enrollments.Any(e => e.StudentId == student.StudentId) && s.ScheduleDate.Date == selectedDate.Date)
                .OrderBy(s => s.CourseSection.Sessions)
                .ToListAsync();

            var listEnrollment = await _context.Enrollments
                .Include(e => e.CourseSection)
                .ThenInclude(cs => cs.Course)
                .Where(e => e.StudentId == student.StudentId && e.Status == EnrollmentStatus.Finished)
                .ToListAsync();

            int totalCredits = listEnrollment.Sum(e => e.CourseSection?.Course?.CourseCredit ?? 0);

            var listScores = await _context.Scores
                .Where(s => s.StudentId == student.StudentId)
                .ToListAsync();

            double TotalWeightScore = 0;

            foreach (var enrollment in listEnrollment)
            {
                var score = listScores.FirstOrDefault(s => s.CourseSectionId == enrollment.CourseSectionId);
                TotalWeightScore += score.FinalScore * enrollment.CourseSection.Course.CourseCredit;
            }

            double totalGPA = 0;

            if (totalCredits > 0)
            {
                totalGPA = TotalWeightScore / totalCredits;
                totalGPA = Math.Round(totalGPA, 2);
            }

            ViewData["TotalScore"] = totalGPA;
            ViewData["TotalCredits"] = totalCredits;

            ViewData["ListSchedule"] = studentSchedule;

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
                .Include(si => si.CourseSection)
                .ThenInclude(cs => cs.Lecturer)
                .Where(si => si.CourseSection.LecturerId == lecturer.LecturerId && si.ScheduleDate.Date == selectedDate.Date)
                .ToList();

            ViewData["ListSchedule"] = schedule;

            var teachingList = _context.CoursesSections
                .Include(cs => cs.Course) 
                .Include(cs => cs.Lecturer)
                .Include(cs => cs.Semester)
                .Where(cs => cs.LecturerId == lecturer.LecturerId
                       && cs.Semester.StartDate <= DateTime.Now
                       && cs.Semester.EndDate >= DateTime.Now)
                .ToList();

            ViewData["teachingList"] = teachingList;

            return View();
        }

        public async Task<ActionResult> AdminIndex(DateTime? date)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
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
                    .Where(e => e.StudentId == currentStudent.StudentId && e.Status == EnrollmentStatus.Finished)
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

                ViewData["TotalScore"] = totalGPA;
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

                ViewData["Name"] = lecturer.User.FullName;
                ViewData["Faculty"] = lecturer.Faculty?.FacultyName ?? "Khoa";
                ViewData["City"] = lecturer.User.City ?? "Chưa có thông tin";
                ViewData["Email"] = lecturer.User.Email;
                ViewData["PhoneNumber"] = lecturer.User.PhoneNumber ?? "Chưa có thông tin";

                var listTeaching = await _context.CoursesSections
                    .Include(cs => cs.Course)
                    .Where(cs => cs.LecturerId == lecturer.LecturerId)
                    .ToListAsync();

                int totalClass = listTeaching.Count();

                ViewData["TotalClass"] = totalClass; 
                ViewData["CourseList"] = listTeaching;   

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
            // Lấy User đang đăng nhập (để bảo mật, tránh sửa hồ sơ người khác qua F12)
            var currentUser = await _userManager.GetUserAsync(User);

            // Lấy dữ liệu gốc từ DB
            var studentInDb = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (studentInDb != null)
            {
                // --- CẬP NHẬT DỮ LIỆU ---
                // Dữ liệu mới nằm trong modelInput.User...

                studentInDb.User.PhoneNumber = modelInput.User.PhoneNumber;
                studentInDb.User.City = modelInput.User.City;

                // [QUAN TRỌNG] Address giờ nằm trong User, không phải Student
                studentInDb.User.Address = modelInput.User.Address;

                // Các trường mới bạn vừa thêm
                studentInDb.User.Country = modelInput.User.Country;

                // Chỉ cập nhật ngày sinh nếu hợp lệ (không phải ngày mặc định MinValue)
                if (modelInput.User.DateOfBirth > DateTime.MinValue)
                {
                    studentInDb.User.DateOfBirth = modelInput.User.DateOfBirth;
                }

                // Lưu thay đổi vào DB
                _context.Update(studentInDb);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }

            // Nếu có lỗi, trả về form cũ
            return PartialView("_EditProfileModal", modelInput);
        }
    }
}
