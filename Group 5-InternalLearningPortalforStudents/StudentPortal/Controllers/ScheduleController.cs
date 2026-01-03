using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {
        private readonly StudentPortalContext _context;
        private readonly UserManager<User> _userManager;

        public ScheduleController(StudentPortalContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ĐIỀU HƯỚNG (INDEX)
        public IActionResult Index()
        {
            if (User.IsInRole("Student"))
            {
                return RedirectToAction(nameof(StudentSchedule));
            }
            if (User.IsInRole("Lecturer"))
            {
                return RedirectToAction(nameof(LecturerSchedule));
            }
            if (User.IsInRole("Admin"))
            {
                // Admin thì sang trang quản lý danh sách lớp
                return RedirectToAction("Index", "CourseSections");
            }
            return RedirectToAction("AccessDenied", "Account");
        }

        // LỊCH HỌC SINH VIÊN (StudentSchedule)
        [Authorize(Roles = "Student")]
        public async Task<ActionResult> StudentSchedule(DateTime? date)
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

        // LỊCH DẠY GIẢNG VIÊN (LecturerSchedule)
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> LecturerSchedule(DateTime? date)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return View("Error");

            // --- Logic tính toán Tuần (Giống SV) ---
            DateTime anchorDate = date ?? DateTime.Today;
            int diff = (7 + (anchorDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = anchorDate.AddDays(-1 * diff).Date;
            DateTime endOfWeek = startOfWeek.AddDays(6).Date;

            // --- Query lấy lịch dạy ---
            var scheduleItems = await _context.ScheduleItems
                .Include(si => si.CourseSection).ThenInclude(cs => cs.Course)
                // Lọc theo LecturerId của lớp học phần
                .Where(si => si.CourseSection.LecturerId == lecturer.LecturerId)
                .Where(si => si.ScheduleDate >= startOfWeek && si.ScheduleDate <= endOfWeek)
                .OrderBy(si => si.ScheduleDate).ThenBy(si => si.CourseSection.DayStart)
                .ToListAsync();

            ViewData["StartOfWeek"] = startOfWeek;
            ViewData["EndOfWeek"] = endOfWeek;
            ViewData["WeekRange"] = $"Từ {startOfWeek:dd/MM/yyyy} đến {endOfWeek:dd/MM/yyyy}";

            ViewData["PrevDate"] = startOfWeek.AddDays(-7).ToString("yyyy-MM-dd");
            ViewData["NextDate"] = startOfWeek.AddDays(7).ToString("yyyy-MM-dd");

            return View(scheduleItems); // Có thể dùng chung View với SV hoặc tạo View riêng
        }
    }
}