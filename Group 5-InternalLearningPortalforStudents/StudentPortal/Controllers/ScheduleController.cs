using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;
using System.Security.Claims;

namespace StudentPortal.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {
        private readonly StudentPortalContext _context;

        public ScheduleController(StudentPortalContext context)
        {
            _context = context;
        }

        // GET: Schedule (Xem thời khóa biểu)
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            List<ScheduleItem> scheduleItems = new List<ScheduleItem>();

            if (User.IsInRole("Student"))
            {
                // Lấy lịch học dựa trên các môn sinh viên đã đăng ký
                scheduleItems = await _context.Enrollments
                    .Where(e => e.Student.UserId == userId)
                    .Include(e => e.CourseSection)
                        .ThenInclude(cs => cs.Course) // Lấy tên môn học
                    .Include(e => e.CourseSection)
                        .ThenInclude(cs => cs.ScheduleItems)
                    .SelectMany(e => e.CourseSection.ScheduleItems)
                    .OrderBy(s => s.Day).ThenBy(s => s.StartTime)
                    .ToListAsync();
            }
            else if (User.IsInRole("Lecturer"))
            {
                // Lấy lịch dạy dựa trên các lớp giảng viên được phân công
                // Giả sử CourseSection có trường LecturerId hoặc quan hệ với User
                scheduleItems = await _context.CoursesSections
                    .Where(cs => cs.Lecturer.UserId == userId) // Cần check lại model CourseSection của bạn
                    .Include(cs => cs.Course)
                    .Include(cs => cs.ScheduleItems)
                    .SelectMany(cs => cs.ScheduleItems)
                    .OrderBy(s => s.Day).ThenBy(s => s.StartTime)
                    .ToListAsync();
            }
            else if (User.IsInRole("Admin"))
            {
                // Admin xem toàn bộ hoặc trang quản lý riêng
                return RedirectToAction(nameof(Manage));
            }

            return View(scheduleItems);
        }

        // GET: Schedule/Manage (Chỉ Admin mới được vào để CRUD)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            var items = await _context.ScheduleItems
                .Include(s => s.CourseSection)
                .ThenInclude(cs => cs.Course)
                .OrderBy(s => s.CourseSection.Course.CourseName)
                .ToListAsync();
            return View(items);
        }

        // GET: Schedule/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            // Dropdown chọn Lớp học phần
            ViewData["CourseSectionId"] = new SelectList(_context.CoursesSections.Include(c => c.Course), "SectionId", "SectionName");
            return View();
        }

        // POST: Schedule/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(ScheduleItem scheduleItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(scheduleItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Manage));
            }
            ViewData["CourseSectionId"] = new SelectList(_context.CoursesSections.Include(c => c.Course), "SectionId", "SectionName", scheduleItem.CourseSectionId);
            return View(scheduleItem);
        }

        // Helper lấy UserId
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }
    }
}