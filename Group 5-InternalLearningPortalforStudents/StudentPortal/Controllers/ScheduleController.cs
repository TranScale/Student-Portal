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

            var semesters = await _context.Semesters.OrderBy(s => s.StartDate).ToListAsync();

            Semester selectedSemester = null;

            if (!semesterId.HasValue && date.HasValue)
            {
                selectedSemester = semesters.FirstOrDefault(s => date.Value >= s.StartDate && date.Value <= s.EndDate);
            }

            if (selectedSemester == null && semesterId.HasValue)
            {
                selectedSemester = semesters.FirstOrDefault(s => s.SemesterId == semesterId);
            }

            if (selectedSemester == null)
            {
                selectedSemester = semesters.FirstOrDefault(s => s.IsActive) ?? semesters.FirstOrDefault();
            }

            if (selectedSemester == null) return View("Error");

            if (date.HasValue)
            {
                if (date.Value < selectedSemester.StartDate || date.Value > selectedSemester.EndDate)
                {
                    date = null; 
                }
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
                if (weekEnd >= selectedSemester.StartDate && loopDate <= selectedSemester.EndDate)
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
                .Where(si => si.ScheduleDate >= startOfWeek && si.ScheduleDate <= endOfWeek)
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

            // 1. Lấy danh sách kỳ học (Mới nhất lên đầu)
            var semesters = await _context.Semesters.OrderBy(s => s.StartDate).ToListAsync();

            // 2. Logic xác định Học Kỳ (SelectedSemester)
            Semester selectedSemester = null;

            // a. Nếu không chọn kỳ nhưng có chọn ngày (VD: bấm nút Hôm nay ở kỳ khác) -> Tìm kỳ chứa ngày đó
            if (!semesterId.HasValue && date.HasValue)
            {
                selectedSemester = semesters.FirstOrDefault(s => date.Value >= s.StartDate && date.Value <= s.EndDate);
            }

            // b. Lấy theo ID người dùng chọn
            if (selectedSemester == null && semesterId.HasValue)
            {
                selectedSemester = semesters.FirstOrDefault(s => s.SemesterId == semesterId);
            }

            // c. Fallback: Kỳ Active hoặc kỳ đầu tiên tìm thấy
            if (selectedSemester == null)
            {
                selectedSemester = semesters.FirstOrDefault(s => s.IsActive) ?? semesters.FirstOrDefault();
            }

            if (selectedSemester == null) return View("Error");

            // 3. Xử lý logic Ngày hiển thị (Anchor Date)
            // Nếu ngày được gửi lên KHÔNG thuộc kỳ đã chọn -> Reset ngày (để về mặc định của kỳ mới)
            if (date.HasValue)
            {
                if (date.Value < selectedSemester.StartDate || date.Value > selectedSemester.EndDate)
                {
                    date = null;
                }
            }

            DateTime anchorDate;
            if (date.HasValue)
            {
                anchorDate = date.Value;
            }
            else
            {
                // Mặc định: Nếu hôm nay nằm trong kỳ -> lấy hôm nay. Không thì lấy ngày bắt đầu kỳ.
                if (DateTime.Today >= selectedSemester.StartDate && DateTime.Today <= selectedSemester.EndDate)
                    anchorDate = DateTime.Today;
                else
                    anchorDate = selectedSemester.StartDate;
            }

            // 4. Chuẩn hóa AnchorDate về THỨ 2 (StartOfWeek)
            int diff = (7 + (anchorDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = anchorDate.AddDays(-1 * diff).Date;
            DateTime endOfWeek = startOfWeek.AddDays(6).Date;

            // 5. Tạo Dropdown Tuần (Weeks List)
            var weeksList = new List<object>();

            // Tìm thứ 2 của tuần bắt đầu kỳ
            int startOffset = (7 + (selectedSemester.StartDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime loopDate = selectedSemester.StartDate.AddDays(-startOffset).Date;

            int weekNumber = 1;
            // Loop phủ hết ngày kết thúc kỳ
            while (loopDate <= selectedSemester.EndDate.AddDays(6))
            {
                DateTime weekEnd = loopDate.AddDays(6);
                // Chỉ thêm vào list nếu tuần này dính dáng tới thời gian học
                if (weekEnd >= selectedSemester.StartDate && loopDate <= selectedSemester.EndDate)
                {
                    string label = $"Tuần {weekNumber} [{loopDate:dd/MM} - {weekEnd:dd/MM}]";
                    if (loopDate == startOfWeek) label += " (Đang chọn)";

                    weeksList.Add(new { Value = loopDate.ToString("yyyy-MM-dd"), Label = label });
                    weekNumber++;
                }
                loopDate = loopDate.AddDays(7);
            }

            // 6. Query dữ liệu (Lọc theo LecturerId)
            var scheduleItems = await _context.ScheduleItems
                .Include(si => si.CourseSection).ThenInclude(cs => cs.Course)
                // Lọc lịch dạy của GIẢNG VIÊN này
                .Where(si => si.CourseSection.LecturerId == lecturer.LecturerId)
                .Where(si => si.ScheduleDate >= startOfWeek && si.ScheduleDate <= endOfWeek)
                .OrderBy(si => si.ScheduleDate).ThenBy(si => si.CourseSection.DayStart)
                .ToListAsync();

            // 7. Truyền dữ liệu ra View
            ViewData["ScheduleList"] = scheduleItems;
            ViewData["StartOfWeek"] = startOfWeek;
            ViewData["WeekRange"] = $"{startOfWeek:dd/MM/yyyy} - {endOfWeek:dd/MM/yyyy}";

            // Dropdown Data
            ViewData["Semesters"] = new SelectList(semesters, "SemesterId", "SemesterName", selectedSemester.SemesterId);
            ViewData["Weeks"] = new SelectList(weeksList, "Value", "Label", startOfWeek.ToString("yyyy-MM-dd"));
            ViewData["CurrentSemesterId"] = selectedSemester.SemesterId;
            ViewData["CurrentDate"] = startOfWeek.ToString("yyyy-MM-dd");

            // Navigation Data
            ViewData["PrevDate"] = startOfWeek.AddDays(-7).ToString("yyyy-MM-dd");
            ViewData["NextDate"] = startOfWeek.AddDays(7).ToString("yyyy-MM-dd");

            return View();
        }
    }
}