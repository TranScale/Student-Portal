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

        // GET: /Schedule
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            List<CourseSection> sections = new List<CourseSection>();

            // TRƯỜNG HỢP 1: SINH VIÊN (Xem lịch các lớp đã đăng ký)
            if (User.IsInRole("Student"))
            {
                // Tìm StudentId từ UserId
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student != null)
                {
                    // Lấy các lớp từ bảng Enrollment
                    // Điều kiện: Status = Approved (Đã duyệt) hoặc Pending (Chờ duyệt)
                    // Không lấy Cancelled
                    sections = await _context.Enrollments
                        .Include(e => e.CourseSection)
                            .ThenInclude(cs => cs.Course) // Lấy tên môn
                        .Include(e => e.CourseSection)
                            .ThenInclude(cs => cs.Lecturer) // Lấy tên GV
                                .ThenInclude(l => l.User)
                        .Where(e => e.StudentId == student.StudentId && e.Status != EnrollmentStatus.Cancelled)
                        .Select(e => e.CourseSection)
                        .ToListAsync();

                    ViewBag.Role = "Student";
                }
            }

            // TRƯỜNG HỢP 2: GIẢNG VIÊN (Xem lịch các lớp mình dạy)
            else if (User.IsInRole("Lecturer"))
            {
                // Tìm LecturerId từ UserId
                var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
                if (lecturer != null)
                {
                    // Lấy các lớp do chính giảng viên này đứng lớp
                    sections = await _context.CoursesSections
                        .Include(cs => cs.Course) // Lấy tên môn
                        .Where(cs => cs.LecturerId == lecturer.LecturerId)
                        .ToListAsync();

                    ViewBag.Role = "Lecturer";
                }
            }
            return View(sections);
        }
    }
}