using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
                return RedirectToAction("Index", "CourseSections");
            }
            return RedirectToAction("AccessDenied", "Account");
        }

        [Authorize(Roles = "Student")]
        public async Task<ActionResult> StudentSchedule(int? semesterId, DateTime? date)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == currentUser.Id);
            if (student == null) return View("Error");

            var semesters = await _context.Semesters.OrderByDescending(s => s.StartDate).ToListAsync();

            Semester selectedSemester = null;

            if (semesterId.HasValue)
            {
                selectedSemester = semesters.FirstOrDefault(s => s.SemesterId == semesterId);
            }

            else if (date.HasValue)
            {
                selectedSemester = semesters.FirstOrDefault(s => date.Value >= s.StartDate && date.Value <= s.EndDate);
            }

            if (selectedSemester == null)
            {
                selectedSemester = semesters.FirstOrDefault(s => s.IsActive) ?? semesters.FirstOrDefault();
            }

            if (selectedSemester == null) return View("Error");

            if (date.HasValue && (date < selectedSemester.StartDate || date > selectedSemester.EndDate))
            {
                date = null;
            }

            DateTime anchorDate;
            if (date.HasValue)
            {
                anchorDate = date.Value;
            }
            else
            {
                if (DateTime.Today >= selectedSemester.StartDate && DateTime.Today <= selectedSemester.EndDate)
                    anchorDate = DateTime.Today;
                else
                    anchorDate = selectedSemester.StartDate;
            }

            int diff = (7 + (anchorDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = anchorDate.AddDays(-1 * diff).Date;
            DateTime endOfWeek = startOfWeek.AddDays(6).Date;

            var weeksList = new List<object>();
            int startOffset = (7 + (selectedSemester.StartDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime loopDate = selectedSemester.StartDate.AddDays(-startOffset).Date;
            int weekNumber = 1;

            while (loopDate <= selectedSemester.EndDate.AddDays(6))
            {
                DateTime weekEnd = loopDate.AddDays(6);
                if (weekEnd >= selectedSemester.StartDate)
                {
                    string label = $"Tuần {weekNumber} [{loopDate:dd/MM} - {weekEnd:dd/MM}]";
                    if (loopDate == startOfWeek) label += " (Đang chọn)";
                    weeksList.Add(new { Value = loopDate.ToString("yyyy-MM-dd"), Label = label });
                    weekNumber++;
                }
                loopDate = loopDate.AddDays(7);
            }

            var scheduleItems = await _context.ScheduleItems
                .Include(si => si.CourseSection).ThenInclude(cs => cs.Course)
                .Include(si => si.CourseSection).ThenInclude(cs => cs.Lecturer).ThenInclude(l => l.User)
                .Where(si => si.CourseSection.Enrollments.Any(e => e.StudentId == student.StudentId))
                .Where(si => si.CourseSection.SemesterId == selectedSemester.SemesterId)
                .Where(si => si.ScheduleDate >= startOfWeek && si.ScheduleDate <= endOfWeek)
                .OrderBy(si => si.ScheduleDate)
                .ToListAsync(); 

            ViewData["ScheduleList"] = scheduleItems;
            ViewData["StartOfWeek"] = startOfWeek;
            ViewData["WeekRange"] = $"{startOfWeek:dd/MM/yyyy} - {endOfWeek:dd/MM/yyyy}";

            ViewData["Semesters"] = new SelectList(semesters, "SemesterId", "SemesterName", selectedSemester.SemesterId);
            ViewData["Weeks"] = new SelectList(weeksList, "Value", "Label", startOfWeek.ToString("yyyy-MM-dd"));

            ViewData["CurrentSemesterId"] = selectedSemester.SemesterId;
            ViewData["CurrentDate"] = startOfWeek.ToString("yyyy-MM-dd");

            ViewData["PrevDate"] = startOfWeek.AddDays(-7).ToString("yyyy-MM-dd");
            ViewData["NextDate"] = startOfWeek.AddDays(7).ToString("yyyy-MM-dd");

            return View();
        }

        // LỊCH DẠY GIẢNG VIÊN (LecturerSchedule)
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> LecturerSchedule(int? semesterId, DateTime? date)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return View("Error");

            // 1. Lấy danh sách học kỳ (Sắp xếp giảm dần để kỳ mới nhất lên đầu)
            var semesters = await _context.Semesters.Where(s => s.IsActive).ToListAsync();

            Semester selectedSemester = null;

            // 2. Xác định Học kỳ (Ưu tiên ID người dùng chọn)
            if (semesterId.HasValue)
            {
                selectedSemester = semesters.FirstOrDefault(s => s.SemesterId == semesterId);
            }
            else if (date.HasValue)
            {
                selectedSemester = semesters.FirstOrDefault(s => date.Value >= s.StartDate && date.Value <= s.EndDate);
            }

            if (selectedSemester == null)
            {
                selectedSemester = semesters.FirstOrDefault(s => s.IsActive) ?? semesters.FirstOrDefault();
            }

            if (selectedSemester == null)
            {
                ViewData["ErrorMessage"] = "Hiện tại chưa có học kỳ nào được kích hoạt. Vui lòng liên hệ Admin.";
                return View();
            }

            // 3. Xử lý logic Ngày hiển thị (Anchor Date)
            if (date.HasValue && (date < selectedSemester.StartDate || date > selectedSemester.EndDate))
            {
                date = null;
            }

            DateTime anchorDate;
            if (date.HasValue)
            {
                anchorDate = date.Value;
            }
            else
            {
                if (DateTime.Today >= selectedSemester.StartDate && DateTime.Today <= selectedSemester.EndDate)
                    anchorDate = DateTime.Today;
                else
                    anchorDate = selectedSemester.StartDate;
            }

            // 4. Tính toán tuần
            int diff = (7 + (anchorDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = anchorDate.AddDays(-1 * diff).Date;
            DateTime endOfWeek = startOfWeek.AddDays(6).Date;

            // 5. Tạo Dropdown Tuần
            var weeksList = new List<object>();
            int startOffset = (7 + (selectedSemester.StartDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime loopDate = selectedSemester.StartDate.AddDays(-startOffset).Date;
            int weekNumber = 1;

            while (loopDate <= selectedSemester.EndDate.AddDays(6))
            {
                DateTime weekEnd = loopDate.AddDays(6);
                if (weekEnd >= selectedSemester.StartDate)
                {
                    string label = $"Tuần {weekNumber} [{loopDate:dd/MM} - {weekEnd:dd/MM}]";
                    if (loopDate == startOfWeek) label += " (Đang chọn)";
                    weeksList.Add(new { Value = loopDate.ToString("yyyy-MM-dd"), Label = label });
                    weekNumber++;
                }
                loopDate = loopDate.AddDays(7);
            }

            // 6. QUERY DỮ LIỆU (ĐÃ THÊM LỌC SEMESTERID)
            var scheduleItems = await _context.ScheduleItems
                .Include(si => si.CourseSection).ThenInclude(cs => cs.Course)
                // Lọc theo Giảng viên
                .Where(si => si.CourseSection.LecturerId == lecturer.LecturerId)
                // [QUAN TRỌNG] Lọc theo Học kỳ đang chọn
                .Where(si => si.CourseSection.SemesterId == selectedSemester.SemesterId)
                // Lọc theo Tuần
                .Where(si => si.ScheduleDate >= startOfWeek && si.ScheduleDate <= endOfWeek)
                .OrderBy(si => si.ScheduleDate)
                .ToListAsync();

            // 7. Truyền dữ liệu ra View
            ViewData["ScheduleList"] = scheduleItems;
            ViewData["StartOfWeek"] = startOfWeek;
            ViewData["WeekRange"] = $"{startOfWeek:dd/MM/yyyy} - {endOfWeek:dd/MM/yyyy}";

            ViewData["Semesters"] = new SelectList(semesters, "SemesterId", "SemesterName", selectedSemester.SemesterId);
            ViewData["Weeks"] = new SelectList(weeksList, "Value", "Label", startOfWeek.ToString("yyyy-MM-dd"));
            ViewData["CurrentSemesterId"] = selectedSemester.SemesterId;
            ViewData["CurrentDate"] = startOfWeek.ToString("yyyy-MM-dd");

            ViewData["PrevDate"] = startOfWeek.AddDays(-7).ToString("yyyy-MM-dd");
            ViewData["NextDate"] = startOfWeek.AddDays(7).ToString("yyyy-MM-dd");

            return View();
        }
    }
}