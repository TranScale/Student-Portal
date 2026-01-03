using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Business;
using StudentPortal.Business.Implementation;
using StudentPortal.Data;
using StudentPortal.Models;
using StudentPortal.Services.Implementations;
using System.Threading.Tasks;

namespace StudentPortal.Controllers.Role
{
    [Authorize]
    public class StudentController : Controller
    {
        //Lấy user hiện tại
        private readonly UserManager<User> _userManager;
        private readonly StudentPortalContext _context;
        private readonly SignInManager<User> _signInManager;


        public StudentController(UserManager<User> userManager, StudentPortalContext context, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _context = context;
            _signInManager = signInManager;
        }

        // GET: StudentController
        public async Task<ActionResult> Index(DateTime? date)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if(currentUser == null)
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


        //Đợi code sau
        public async Task<ActionResult> Profile()
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
            catch(Exception)
            { return View("Error"); }
            
        }

        public async Task<IActionResult> Score(int? semesterId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if(currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == currentUser.Id);
                if(student == null)
                {
                    return View("Error");
                }

                DateTime studentDateStudy = student.StartStudyDate.ToDateTime(TimeOnly.MinValue);

                var semester = await _context.Semesters
                    .Where(s => s.StartDate.Date >= studentDateStudy)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => new
                    {
                        Id = s.SemesterId,
                        DisplayText = $"{s.SemesterName} - Năm học {s.AcademicYear}"
                    }).ToListAsync();
                int selectedValue = semesterId ?? (semester.FirstOrDefault()?.Id ?? 1);
                ViewData["SemesterList"] = new SelectList(semester, "Id", "DisplayText",selectedValue);

                var currentSemester = semester.FirstOrDefault(s => s.Id == selectedValue);
                ViewData["CurrentSemesterName"] = currentSemester?.DisplayText;

                var scoreList = await _context.Scores
                    .Include(s => s.CourseSection)
                    .ThenInclude(cs => cs.Course)
                    .Where(s => s.StudentId == student.StudentId && s.CourseSection.SemesterId == selectedValue)
                    .ToListAsync();

                ViewData["ListScores"] = scoreList;

                return View();
            }
            catch(Exception)
            {
                return View("Error");
            }
        }

        public async Task<ActionResult> ScheduleAsync(DateTime? date)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == currentUser.Id);
            if (student == null) return View("Error");

            DateTime anchorDate = date ?? DateTime.Today;

            int diff = (7 + (anchorDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = anchorDate.AddDays(-1 * diff).Date;
            DateTime endOfWeek = startOfWeek.AddDays(6).Date;

            var scheduleItems = await _context.ScheduleItems
                .Include(si => si.CourseSection).ThenInclude(cs => cs.Course)
                .Include(si => si.CourseSection).ThenInclude(cs => cs.Lecturer).ThenInclude(l => l.User)
                .Where(si => si.CourseSection.Enrollments.Any(e => e.StudentId == student.StudentId))
                .Where(si => si.ScheduleDate >= startOfWeek && si.ScheduleDate <= endOfWeek)
                .ToListAsync();

            ViewData["ScheduleList"] = scheduleItems;
            ViewData["StartOfWeek"] = startOfWeek;
            ViewData["WeekRange"] = $"{startOfWeek:dd/MM/yyyy} - {endOfWeek:dd/MM/yyyy}";

            ViewData["PrevDate"] = startOfWeek.AddDays(-7).ToString("yyyy-MM-dd");
            ViewData["NextDate"] = startOfWeek.AddDays(7).ToString("yyyy-MM-dd");

            return View();
        }

        public async Task<ActionResult> Enrollment()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == currentUser.Id);
            if (student == null) return View("Error");

            // 1. Lấy học kỳ đang mở đăng ký (IsActive = true)
            var activeSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive);
            if (activeSemester == null)
            {
                ViewBag.Message = "Hiện chưa có học kỳ nào mở đăng ký.";
                return View();
            }

            // 2. Lấy danh sách các môn ĐÃ ĐĂNG KÝ (Enrollments)
            var registeredList = await _context.Enrollments
                .Include(e => e.CourseSection).ThenInclude(cs => cs.Course)
                .Include(e => e.CourseSection).ThenInclude(cs => cs.Lecturer).ThenInclude(l => l.User)
                .Where(e => e.StudentId == student.StudentId && e.CourseSection.SemesterId == activeSemester.SemesterId)
                .ToListAsync();

            // Lấy danh sách ID các lớp đã đăng ký để loại trừ ở bảng trên
            var registeredSectionIds = registeredList.Select(e => e.CourseSectionId).ToList();

            // 3. Lấy danh sách môn CHỜ ĐĂNG KÝ (CourseSections)
            // Điều kiện: Thuộc học kỳ này AND Chưa nằm trong danh sách đã đăng ký
            var availableList = await _context.CoursesSections
                .Include(cs => cs.Course)
                .Include(cs => cs.Lecturer).ThenInclude(l => l.User)
                .Include(cs => cs.Enrollments) // Để đếm số lượng đã đăng ký (Capacity)
                .Where(cs => cs.SemesterId == activeSemester.SemesterId && !registeredSectionIds.Contains(cs.CourseSectionId))
                .ToListAsync();

            ViewData["RegisteredList"] = registeredList;
            ViewData["AvailableList"] = availableList;
            ViewData["SemesterName"] = activeSemester.SemesterName;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Bảo mật: Chống giả mạo request
        public async Task<IActionResult> CancelRegistration(int id)
        {
            // 1. Lấy thông tin sinh viên hiện tại
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == currentUser.Id);
            if (student == null) return View("Error");

            // 2. Tìm bản ghi đăng ký trong database
            var enrollment = await _context.Enrollments
                .Include(e => e.CourseSection) // Kèm thông tin lớp để check học kỳ nếu cần
                .FirstOrDefaultAsync(e => e.EnrollmentId == id);

            // 3. Kiểm tra hợp lệ
            if (enrollment == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin môn học.";
                return RedirectToAction(nameof(Enrollment));
            }

            // SECURITY CHECK: Chỉ cho phép xóa nếu môn này thuộc về sinh viên hiện tại
            if (enrollment.StudentId != student.StudentId)
            {
                return Unauthorized(); // Hoặc Redirect báo lỗi
            }

            // (Tùy chọn) Kiểm tra xem Học kỳ còn mở không (IsActive)
            // var semester = await _context.Semesters.FindAsync(enrollment.CourseSection.SemesterId);
            // if (!semester.IsActive) { ... báo lỗi ... }

            // 4. Xóa và Lưu
            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hủy môn học thành công!";
            return RedirectToAction(nameof(Enrollment));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRegistration(List<int> selectedCourses)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (selectedCourses == null || selectedCourses.Count == 0)
            {
                TempData["Error"] = "Bạn chưa chọn môn học nào.";
                return RedirectToAction(nameof(Enrollment));
            }

            int successCount = 0;

            foreach (var sectionId in selectedCourses)
            {
                // 1. Kiểm tra lớp học có tồn tại và còn chỗ không
                var section = await _context.CoursesSections
                    .Include(cs => cs.Enrollments)
                    .FirstOrDefaultAsync(cs => cs.CourseSectionId == sectionId);

                if (section != null)
                {
                    // Check sĩ số (Tránh trường hợp full chỗ ngay lúc bấm)
                    if (section.Enrollments.Count >= section.Capacity)
                    {
                        // Bỏ qua môn này hoặc thông báo lỗi
                        continue;
                    }

                    // Check đã đăng ký chưa (tránh trùng lặp)
                    bool isAlreadyEnrolled = await _context.Enrollments
                        .AnyAsync(e => e.StudentId == student.StudentId && e.CourseSectionId == sectionId);

                    if (!isAlreadyEnrolled)
                    {
                        // 2. Tạo bản ghi đăng ký mới
                        var newEnrollment = new Enrollment
                        {
                            StudentId = student.StudentId,
                            CourseSectionId = sectionId,
                        };

                        _context.Enrollments.Add(newEnrollment);
                        successCount++;
                    }
                }
            }

            await _context.SaveChangesAsync();

            if (successCount > 0)
                TempData["Success"] = $"Đăng ký thành công {successCount} môn học.";
            else
                TempData["Error"] = "Đăng ký thất bại hoặc lớp đã đầy.";

            return RedirectToAction(nameof(Enrollment));
        }

    }
}
