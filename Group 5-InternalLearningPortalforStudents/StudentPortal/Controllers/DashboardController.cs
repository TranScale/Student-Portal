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


        public DashboardController(UserManager<User> userManager, StudentPortalContext context, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _context = context;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction(nameof(StudentIndex));
            }
            if (User.IsInRole("Lecturer"))
            {
                return RedirectToAction(nameof(LecturerIndex));
            }

            if (User.IsInRole("Student"))
            {
                return RedirectToAction("StudentIndex", "Dashboard");
            }
            return View();
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
                return View();
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

            ViewData["ListSchedule"] = studentSchedule;

            return View();
        }

        [Authorize(Roles = "Lecturer")] // Chỉ giảng viên mới được vào
        public async Task<IActionResult> LecturerIndex(DateTime? date)
        {
            // 1. Kiểm tra đăng nhập
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. Lấy thông tin Giảng viên
            // SỬA: Lecturer quan hệ với Faculty (Khoa), không phải Department (Ngành)
            // SỬA: Phải Include("User") để lấy FullName, Phone vì bảng Lecturer không lưu mấy cái đó
            var lecturer = await _context.Lecturers
                .Include(l => l.User)     // Lấy thông tin cá nhân
                .Include(l => l.Faculty)  // Lấy thông tin Khoa
                .FirstOrDefaultAsync(l => l.UserId == currentUser.Id);

            if (lecturer == null)
            {
                ViewData["FullName"] = currentUser.FullName;
                ViewData["LecturerCode"] = "Chưa cập nhật";
                return View();
            }

            // 3. Đẩy dữ liệu sang View
            // SỬA: Lấy từ lecturer.User... thay vì lecturer...
            ViewData["FullName"] = lecturer.User.FullName ?? currentUser.FullName;
            ViewData["Email"] = lecturer.User.Email ?? currentUser.Email;
            ViewData["Phone"] = lecturer.User.PhoneNumber;

            // SỬA: Model Lecturer không có LecturerCode, dùng tạm UserName hoặc Id
            ViewData["LecturerCode"] = lecturer.User.UserName ?? "GV" + lecturer.LecturerId;

            // SỬA: Lecturer thuộc về Faculty (Khoa)
            ViewData["Department"] = lecturer.Faculty?.FacultyName ?? "Khoa chưa xác định";

            // 4. XỬ LÝ LỊCH
            DateTime selectedDate = date ?? DateTime.Today;
            ViewData["selectedDate"] = selectedDate;

            var lecturerSchedule = await _context.ScheduleItems
                .Include(s => s.CourseSection).ThenInclude(cs => cs.Course)
                .Where(s => s.CourseSection.LecturerId == lecturer.LecturerId
                            && s.ScheduleDate.Date == selectedDate.Date)
                .OrderBy(s => s.CourseSection.Sessions)
                .ToListAsync();

            ViewData["ListSchedule"] = lecturerSchedule;

            // 5. XỬ LÝ DANH SÁCH LỚP HỌC (TeachingList)
            var teachingList = await _context.CoursesSections
                .Include(cs => cs.Course)
                .Include(cs => cs.Enrollments) // Include để đếm số sinh viên
                .Where(cs => cs.LecturerId == lecturer.LecturerId)
                .ToListAsync(); // Lấy về list trước rồi mới Select để tránh lỗi dịch Enum sang SQL

            // Map dữ liệu sang object dynamic cho View dễ dùng
            ViewBag.TeachingList = teachingList.Select(cs => new
            {
                SubjectName = cs.Course.CourseName,
                // SỬA: Model không có SectionCode, tự tạo mã lớp hiển thị từ Mã Môn + ID
                ClassCode = $"{cs.Course.CourseCode}.{cs.CourseSectionId:D2}",
                // SỬA: Dùng Enum ClassDays và StudySessions convert sang string
                Time = $"{cs.Days} ({cs.Sessions})",
                Room = cs.Room,
                Students = cs.Enrollments.Count()
            }).ToList();

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
                ViewData["Facuty"] = currentStudent.Department.Faculty.FacultyName;

                var listEnrollment = await _context.Enrollments
                    .Include(e => e.CourseSection)
                    .ThenInclude(cs => cs.Course)
                    .Where(e => e.StudentId == currentStudent.StudentId)
                    .ToListAsync();

                int totalCredits = listEnrollment.Sum(e => e.CourseSection?.Course?.CourseCredit ?? 0);
                ViewData["TotalCredits"] = totalCredits;
                ViewData["CourseList"] = listEnrollment;
                return View();
            }
            catch (Exception)
            { return View("Error"); }
        }

        [Authorize(Roles = "Lecturer")]
        public async Task<ActionResult> LecturerProfile()
        {
            try
            {
                // 1. Lấy User đang đăng nhập
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return RedirectToAction("Login", "Account");

                // 2. Lấy thông tin Giảng viên (Kèm thông tin Khoa và User)
                // Lưu ý: Giảng viên thường trực thuộc Khoa (Faculty), ít khi qua Department như Student
                var currentLecturer = await _context.Lecturers
                    .Include(l => l.Faculty)
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.UserId == currentUser.Id);

                if (currentLecturer == null) return View("Error");

                // 3. Đổ dữ liệu ra ViewData (Giữ nguyên tên key để tái sử dụng View nếu cần)
                ViewData["Name"] = currentLecturer.User.FullName;

                // Giảng viên thường không có "Lớp sinh hoạt" hay "Ngành" cụ thể như SV, 
                // nên ta để hiển thị tên Khoa hoặc để trống.
                ViewData["Department"] = currentLecturer.Faculty?.FacultyName ?? "Bộ môn chung";

                ViewData["City"] = currentLecturer.User.City ?? "Chưa có thông tin";
                ViewData["Email"] = currentLecturer.User.Email;
                ViewData["Code"] = null; // Đổi thành mã GV
                ViewData["PhoneNumber"] = currentLecturer.User.PhoneNumber ?? "Chưa có thông tin";
                ViewData["Facuty"] = currentLecturer.Faculty?.FacultyName;

                // 4. Lấy danh sách lớp HỌC PHẦN đang DẠY (Thay vì Enrollment)
                var listTeaching = await _context.CoursesSections
                    .Include(cs => cs.Course)
                    .Where(cs => cs.LecturerId == currentLecturer.LecturerId)
                    .ToListAsync();

                // 5. Tính toán (Thay vì tổng tín chỉ tích lũy, ta tính tổng tín chỉ đang giảng dạy)
                int totalCredits = listTeaching.Sum(cs => cs.Course?.CourseCredit ?? 0);

                ViewData["TotalCredits"] = totalCredits; // Tổng số tín chỉ đang dạy
                ViewData["CourseList"] = listTeaching;   // Danh sách lớp đang dạy

                return View();
            }
            catch (Exception)
            {
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string? FullName, string? PhoneNumber, string? Email)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    await _signInManager.SignOutAsync();
                    return Redirect("/Account/Login");
                }

                bool hasChanges = false;

                if (!string.IsNullOrEmpty(FullName) && currentUser.FullName != FullName)
                {
                    currentUser.FullName = FullName;
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(PhoneNumber) && currentUser.PhoneNumber != PhoneNumber)
                {
                    currentUser.PhoneNumber = PhoneNumber;
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(Email) && currentUser.Email != Email)
                {
                    currentUser.Email = Email;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    var result = await _userManager.UpdateAsync(currentUser);
                    if (result.Succeeded)
                    {
                        TempData["Success"] = "Cập nhật thông tin thành công!";
                    }
                    else
                    {
                        TempData["Error"] = "Có lỗi xảy ra khi lưu dữ liệu.";
                    }
                }
            }
            catch (Exception ex)
            {
                return View("Error");
            }

            return RedirectToAction("Index");
        }
    }
}
