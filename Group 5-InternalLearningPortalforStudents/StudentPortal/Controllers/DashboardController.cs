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

                return View();
            }
            catch (Exception)
            { return View("Error"); }
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
