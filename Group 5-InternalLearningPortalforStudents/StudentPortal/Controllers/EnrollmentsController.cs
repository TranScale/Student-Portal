using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    [Authorize]
    public class EnrollmentsController : Controller
    {
        private readonly StudentPortalContext _context;
        private readonly UserManager<User> _userManager;

        public EnrollmentsController(StudentPortalContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Danh sách môn đã đăng ký
        public async Task<IActionResult> Index()
        {
            var studentId = await GetCurrentStudentId();
            if (studentId == 0) return RedirectToAction("AccessDenied", "Account");

            var myEnrollments = await _context.Enrollments
                .Include(e => e.CourseSection).ThenInclude(cs => cs.Course)
                .Include(e => e.CourseSection).ThenInclude(cs => cs.Lecturer).ThenInclude(l => l.User)
                .Where(e => e.StudentId == studentId)
                .OrderByDescending(e => e.EnrollmentId)
                .ToListAsync();

            return View(myEnrollments);
        }

        // 2. Trang đăng ký môn (Chọn lớp)
        public async Task<IActionResult> Register()
        {
            var studentId = await GetCurrentStudentId();

            // Lấy lớp học phần thuộc Học kỳ đang Active (Nếu có logic Semester)
            var allSections = await _context.CoursesSections
                .Include(c => c.Course)
                .Include(c => c.Lecturer).ThenInclude(l => l.User)
                //.Where(c => c.Semester.IsActive) // Mở comment nếu muốn lọc theo kỳ
                .ToListAsync();

            var enrolledSectionIds = await _context.Enrollments
                .Where(e => e.StudentId == studentId && e.Status != EnrollmentStatus.Cancelled)
                .Select(e => e.CourseSectionId)
                .ToListAsync();

            // Loại bỏ lớp đã đăng ký
            var availableSections = allSections
                .Where(s => !enrolledSectionIds.Contains(s.CourseSectionId))
                .ToList();

            return View(availableSections);
        }

        // 3. Xử lý logic Đăng Ký (QUAN TRỌNG)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRegistration(int id) // id = CourseSectionId
        {
            var studentId = await GetCurrentStudentId();

            // Check lớp tồn tại
            var section = await _context.CoursesSections.FindAsync(id);
            if (section == null) return NotFound();

            // Check đã đăng ký chưa
            bool exists = await _context.Enrollments
                .AnyAsync(e => e.StudentId == studentId && e.CourseSectionId == id && e.Status != EnrollmentStatus.Cancelled);

            if (exists)
            {
                TempData["Error"] = "Bạn đã đăng ký lớp này rồi.";
                return RedirectToAction(nameof(Register));
            }

            // --- CHECK SĨ SỐ (CAPACITY) ---
            int currentCount = await _context.Enrollments
                .CountAsync(e => e.CourseSectionId == id && e.Status != EnrollmentStatus.Cancelled);

            if (currentCount >= section.Capacity)
            {
                TempData["Error"] = $"Lớp đã đầy ({currentCount}/{section.Capacity}).";
                return RedirectToAction(nameof(Register));
            }

            // --- DÙNG TRANSACTION ---
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // A. Tạo Enrollment
                    var enrollment = new Enrollment
                    {
                        StudentId = studentId,
                        CourseSectionId = id,
                        Status = EnrollmentStatus.Pending // Hoặc Approved tùy quy trình
                    };
                    _context.Add(enrollment);
                    await _context.SaveChangesAsync();

                    // B. Tạo bảng điểm rỗng (Để tránh lỗi null sau này)
                    var score = new Score
                    {
                        StudentId = studentId,
                        CourseSectionId = id,
                        LecturerId = section.LecturerId,
                        ProcessScore = 0,
                        MiddleScore = 0,
                        ExamScore = 0,
                        Value = ScoreValues.F // Mặc định
                    };
                    _context.Add(score);
                    await _context.SaveChangesAsync();

                    transaction.Commit();
                    TempData["Success"] = "Đăng ký thành công!";
                }
                catch
                {
                    transaction.Rollback();
                    TempData["Error"] = "Lỗi hệ thống khi đăng ký.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // 4. Hủy đăng ký
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRegistration(int id)
        {
            var studentId = await GetCurrentStudentId();
            var enrollment = await _context.Enrollments.FindAsync(id);

            if (enrollment != null && enrollment.StudentId == studentId)
            {
                // Xóa mềm hoặc xóa cứng tùy bạn, ở đây set status Cancelled
                enrollment.Status = EnrollmentStatus.Cancelled;

                // Xóa score nếu có
                var score = await _context.Scores
                    .FirstOrDefaultAsync(s => s.StudentId == studentId && s.CourseSectionId == enrollment.CourseSectionId);
                if (score != null) _context.Scores.Remove(score);

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã hủy đăng ký.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<int> GetCurrentStudentId()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return 0;
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            return student?.StudentId ?? 0;
        }
    }
}