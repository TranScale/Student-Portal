using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Cần thêm cái này
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
        private readonly UserManager<User> _userManager; // Dùng để quản lý User

        // Inject thêm UserManager vào Constructor
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
                .Include(e => e.CourseSection)
                    .ThenInclude(cs => cs.Course)
                .Include(e => e.CourseSection)
                    .ThenInclude(cs => cs.Lecturer)
                        .ThenInclude(l => l.User)
                .Where(e => e.StudentId == studentId)
                .OrderByDescending(e => e.EnrollmentId)
                .ToListAsync();

            return View(myEnrollments);
        }

        // 2. Trang đăng ký môn (Chọn lớp)
        public async Task<IActionResult> Register()
        {
            var studentId = await GetCurrentStudentId();

            // Lấy tất cả lớp học phần đang mở
            var allSections = await _context.CoursesSections
                .Include(c => c.Course)
                .Include(c => c.Lecturer).ThenInclude(l => l.User)
                .ToListAsync();

            // Lấy danh sách các lớp đã đăng ký rồi
            var enrolledSectionIds = await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseSectionId)
                .ToListAsync();

            // Loại bỏ các lớp đã học, chỉ hiện lớp mới
            var availableSections = allSections
                .Where(s => !enrolledSectionIds.Contains(s.CourseSectionId))
                .ToList();

            return View(availableSections);
        }

        // 3. Xử lý logic Đăng Ký
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRegistration(int id) // id = CourseSectionId
        {
            var studentId = await GetCurrentStudentId();

            // Kiểm tra lớp có tồn tại không
            var section = await _context.CoursesSections.FindAsync(id);
            if (section == null) return NotFound();

            // Kiểm tra trùng lặp
            bool exists = await _context.Enrollments
                .AnyAsync(e => e.StudentId == studentId && e.CourseSectionId == id);

            if (!exists)
            {
                // A. Tạo Enrollment
                var enrollment = new Enrollment
                {
                    StudentId = studentId,
                    CourseSectionId = id,
                    //Status = EnrollmentStatus.Pending // Chờ duyệt
                };
                _context.Add(enrollment);

                // B. Tạo bảng điểm rỗng (Để GV nhập điểm sau này)
                var score = new Score
                {
                    StudentId = studentId,
                    CourseSectionId = id,
                    LecturerId = section.LecturerId, // Gán GV của lớp đó vào bảng điểm
                    ProcessScore = 0,
                    MiddleScore = 0,
                    ExamScore = 0,
                    Value = ScoreValues.F
                };
                _context.Add(score);

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đăng ký thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // 4. Hủy đăng ký
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRegistration(int id) // id = EnrollmentId
        {
            var studentId = await GetCurrentStudentId();

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.EnrollmentId == id && e.StudentId == studentId);

            if (enrollment != null)
            {
                // Xóa điểm trước (nếu có)
                var score = await _context.Scores
                    .FirstOrDefaultAsync(s => s.StudentId == studentId && s.CourseSectionId == enrollment.CourseSectionId);

                if (score != null) _context.Scores.Remove(score);

                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã hủy học phần.";
            }

            return RedirectToAction(nameof(Index));
        }

        // --- HELPER QUAN TRỌNG: Lấy StudentId từ Identity ---
        private async Task<int> GetCurrentStudentId()
        {
            // Lấy User đang đăng nhập bằng UserManager
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return 0;

            // Tìm thông tin Sinh viên dựa trên UserId
            // Lưu ý: user.Id là int (do bạn khai báo IdentityUser<int>)
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == user.Id); // Hoặc user.UserId tùy model User của bạn

            return student?.StudentId ?? 0;
        }
    }
}