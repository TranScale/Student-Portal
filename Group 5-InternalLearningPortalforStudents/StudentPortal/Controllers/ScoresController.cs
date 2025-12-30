using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    [Authorize]
    public class ScoreController : Controller
    {
        private readonly StudentPortalContext _context;
        private readonly UserManager<User> _userManager;

        public ScoreController(StudentPortalContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Xem danh sách lớp (GV) hoặc Xem điểm (SV)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (User.IsInRole("Student"))
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                var scores = await _context.Scores
                    .Include(s => s.CourseSection).ThenInclude(c => c.Course)
                    .Include(s => s.Lecturer).ThenInclude(l => l.User)
                    .Where(s => s.StudentId == student.StudentId)
                    .ToListAsync();
                return View("StudentScore", scores);
            }
            else if (User.IsInRole("Lecturer"))
            {
                var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
                // Lấy các lớp giảng viên này dạy
                var sections = await _context.CoursesSections
                    .Include(c => c.Course)
                    .Where(c => c.LecturerId == lecturer.LecturerId)
                    .ToListAsync();
                return View("LecturerClassList", sections);
            }
            return RedirectToAction("AccessDenied", "Account");
        }

        // 2. Form Nhập Điểm (GET)
        [Authorize(Roles = "Lecturer")]
        [HttpGet]
        public async Task<IActionResult> EnterGrades(int sectionId)
        {
            // Security Check: Có phải lớp của GV này không?
            var user = await _userManager.GetUserAsync(User);
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);

            var section = await _context.CoursesSections.FindAsync(sectionId);
            if (section == null || section.LecturerId != lecturer.LecturerId) return Forbid();

            // Lấy danh sách điểm
            var scores = await _context.Scores
                .Include(s => s.Student).ThenInclude(st => st.User)
                .Where(s => s.CourseSectionId == sectionId)
                .ToListAsync();

            // Nếu danh sách điểm trống (do lỗi lúc đăng ký chưa tạo), tạo bổ sung

            ViewBag.SectionId = sectionId;
            return View(scores);
        }

        // 3. Lưu Điểm (POST)
        [Authorize(Roles = "Lecturer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnterGrades(List<Score> models, int sectionId)
        {
            // Security Check lại lần nữa
            var user = await _userManager.GetUserAsync(User);
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            var isMySection = await _context.CoursesSections.AnyAsync(c => c.CourseSectionId == sectionId && c.LecturerId == lecturer.LecturerId);

            if (!isMySection) return Forbid();

            foreach (var item in models)
            {
                var scoreDb = await _context.Scores.FindAsync(item.ScoreId);
                if (scoreDb != null)
                {
                    // Cập nhật điểm thành phần
                    scoreDb.ProcessScore = Math.Clamp(item.ProcessScore, 0, 10);
                    scoreDb.MiddleScore = Math.Clamp(item.MiddleScore, 0, 10);
                    scoreDb.ExamScore = Math.Clamp(item.ExamScore, 0, 10);

                    // TÍNH ĐIỂM TỔNG KẾT & XẾP LOẠI
                    float total = (scoreDb.ProcessScore * 0.3f) + (scoreDb.MiddleScore * 0.2f) + (scoreDb.ExamScore * 0.5f);

                    if (total >= 8.5) scoreDb.Value = ScoreValues.A;
                    else if (total >= 7.0) scoreDb.Value = ScoreValues.B;
                    else if (total >= 5.5) scoreDb.Value = ScoreValues.C;
                    else if (total >= 4.0) scoreDb.Value = ScoreValues.D;
                    else scoreDb.Value = ScoreValues.F;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã lưu bảng điểm thành công!";
            return RedirectToAction(nameof(EnterGrades), new { sectionId = sectionId });
        }
    }
}