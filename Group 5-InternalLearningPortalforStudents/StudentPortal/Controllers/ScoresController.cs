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

        // 1. TRANG CHỦ (Điều hướng)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // --- SINH VIÊN: Xem điểm của mình ---
            if (User.IsInRole("Student"))
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student == null) return View("Error");

                // Lấy danh sách điểm từ bảng Score
                var myScores = await _context.Scores
                    .Include(s => s.CourseSection).ThenInclude(cs => cs.Course)
                    .Include(s => s.Lecturer).ThenInclude(l => l.User)
                    .Where(s => s.StudentId == student.StudentId)
                    .ToListAsync();

                ViewBag.Role = "Student";
                return View(myScores); // Trả về List<Score>
            }

            // --- GIẢNG VIÊN: Chọn lớp để nhập điểm ---
            if (User.IsInRole("Lecturer"))
            {
                var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
                if (lecturer == null) return View("Error");

                // Lấy danh sách các lớp GV này dạy
                var sections = await _context.CoursesSections
                    .Include(cs => cs.Course)
                    .Where(cs => cs.LecturerId == lecturer.LecturerId)
                    .ToListAsync();

                ViewBag.Role = "Lecturer";
                return View(sections); // Trả về List<CourseSection>
            }

            return RedirectToAction("AccessDenied", "Account");
        }

        // 2. FORM NHẬP ĐIỂM (GET)
        [Authorize(Roles = "Lecturer")]
        [HttpGet]
        public async Task<IActionResult> EnterGrades(int sectionId)
        {
            // Lấy danh sách sinh viên ĐANG HỌC lớp này (Enrollment)
            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Student).ThenInclude(s => s.User)
                //.Where(e => e.CourseSectionId == sectionId && e.Status == EnrollmentStatus.Approved)
                .Where(e => e.CourseSectionId == sectionId)
                .OrderBy(e => e.Student.StudentCode)
                .ToListAsync();

            // Lấy danh sách điểm ĐÃ CÓ trong DB (Score)
            var existingScores = await _context.Scores
                .Where(s => s.CourseSectionId == sectionId)
                .ToListAsync();

            // TẠO DANH SÁCH VIEW MODEL ĐỂ HIỂN THỊ
            // Duyệt qua từng sinh viên, nếu có điểm rồi thì điền vào, chưa có thì tạo mới
            var modelList = new List<Score>();

            foreach (var enrollment in enrollments)
            {
                var score = existingScores.FirstOrDefault(s => s.StudentId == enrollment.StudentId);

                if (score == null)
                {
                    // Nếu chưa có điểm -> Tạo object ảo để hiển thị trên form
                    score = new Score
                    {
                        StudentId = enrollment.StudentId,
                        Student = enrollment.Student, // Gán để lấy tên hiển thị
                        CourseSectionId = sectionId,
                        ScoreId = 0, // Đánh dấu là chưa có trong DB
                        ProcessScore = 0,
                        MiddleScore = 0,
                        ExamScore = 0
                    };
                }
                else
                {
                    // Gán lại Student object để View hiển thị được Tên SV
                    score.Student = enrollment.Student;
                }

                modelList.Add(score);
            }

            ViewBag.SectionId = sectionId;
            return View(modelList);
        }

        // 3. LƯU ĐIỂM (POST)
        [Authorize(Roles = "Lecturer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnterGrades(List<Score> models, int sectionId)
        {
            var user = await _userManager.GetUserAsync(User);
            // Cần kiểm tra null cho user và lecturer để tránh lỗi runtime
            if (user == null) return RedirectToAction("Login", "Account");

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return Forbid(); // Hoặc xử lý lỗi phù hợp

            if (models != null && models.Count > 0)
            {
                foreach (var item in models)
                {
                    // Quá trình 30% + Giữa kỳ 20% + Cuối kỳ 50%
                    float total = (item.ProcessScore * 0.3f) + (item.MiddleScore * 0.2f) + (item.ExamScore * 0.5f);

                    // Tính toán ScoreValues dựa trên total
                    ScoreValues calculatedGrade;

                    if (total >= 8.5)
                        calculatedGrade = ScoreValues.A;
                    else if (total >= 7.0)
                        calculatedGrade = ScoreValues.B;
                    else if (total >= 5.5)
                        calculatedGrade = ScoreValues.C;
                    else if (total >= 4.0)
                        calculatedGrade = ScoreValues.D;
                    else
                        calculatedGrade = ScoreValues.F;

                    // LƯU VÀO DB
                    if (item.ScoreId == 0)
                    {
                        // == INSERT ==
                        // Chỉ lưu nếu sinh viên có ít nhất một đầu điểm > 0 hoặc giảng viên cố tình nhập 0
                        if (item.ProcessScore >= 0 || item.MiddleScore >= 0 || item.ExamScore >= 0)
                        {
                            var newScore = new Score
                            {
                                CourseSectionId = sectionId,
                                StudentId = item.StudentId,
                                LecturerId = lecturer.LecturerId,
                                ProcessScore = item.ProcessScore,
                                MiddleScore = item.MiddleScore,
                                ExamScore = item.ExamScore,
                                Value = calculatedGrade // <--- Gán giá trị đã tính toán
                            };
                            _context.Scores.Add(newScore);
                        }
                    }
                    else
                    {
                        // == UPDATE ==
                        var scoreInDb = await _context.Scores.FindAsync(item.ScoreId);
                        if (scoreInDb != null)
                        {
                            scoreInDb.ProcessScore = item.ProcessScore;
                            scoreInDb.MiddleScore = item.MiddleScore;
                            scoreInDb.ExamScore = item.ExamScore;
                            scoreInDb.Value = calculatedGrade; // <--- Cập nhật xếp loại mới
                            scoreInDb.LecturerId = lecturer.LecturerId; // Cập nhật người sửa
                        }
                    }
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã lưu bảng điểm và xếp loại thành công!";
            }

            return RedirectToAction(nameof(EnterGrades), new { sectionId = sectionId });
        }
    }
}