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

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            List<CourseSection> schedules = new List<CourseSection>();

            // 1. NẾU LÀ SINH VIÊN -> Xem Lịch Học
            if (User.IsInRole("Student"))
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student != null)
                {
                    // Lấy các lớp đã đăng ký (trừ lớp đã hủy)
                    schedules = await _context.Enrollments
                        .Where(e => e.StudentId == student.StudentId && e.Status != EnrollmentStatus.Cancelled)
                        .Include(e => e.CourseSection).ThenInclude(cs => cs.Course)
                        .Include(e => e.CourseSection).ThenInclude(cs => cs.Lecturer).ThenInclude(l => l.User)
                        .Select(e => e.CourseSection)
                        .ToListAsync();

                    ViewBag.Title = "Lịch Học Của Tôi";
                    ViewBag.Role = "Student";
                }
            }
            // 2. NẾU LÀ GIẢNG VIÊN -> Xem Lịch Dạy
            else if (User.IsInRole("Lecturer"))
            {
                var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
                if (lecturer != null)
                {
                    // Lấy các lớp mình đứng lớp
                    schedules = await _context.CoursesSections
                        .Where(cs => cs.LecturerId == lecturer.LecturerId)
                        .Include(cs => cs.Course)
                        .ToListAsync();

                    ViewBag.Title = "Lịch Dạy Của Tôi";
                    ViewBag.Role = "Lecturer";
                }
            }
            // 3. NẾU LÀ ADMIN -> Chuyển sang trang CRUD
            else if (User.IsInRole("Admin"))
            {
                // Chuyển hướng sang Controller quản lý lớp học (Bạn phải tạo Controller này nhé)
                return RedirectToAction("Index", "CourseSections");
            }

            return View(schedules);
        }
    }
}