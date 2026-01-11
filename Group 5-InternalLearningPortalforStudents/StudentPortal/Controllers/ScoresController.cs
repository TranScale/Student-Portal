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

            // --- TRƯỜNG HỢP: GIẢNG VIÊN ---
            if (User.IsInRole("Lecturer"))
            {
                var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
                if (lecturer == null) return View("Error");

                // 1. Chỉ lấy duy nhất Học kỳ đang Active
                var activeSemester = await _context.Semesters
                    .FirstOrDefaultAsync(s => s.IsActive);

                // Nếu không có học kỳ nào đang kích hoạt
                if (activeSemester == null)
                {
                    ViewBag.CurrentSemesterName = "Hiện không có học kỳ nào đang diễn ra";
                    return View("LecturerIndex", new List<CourseSection>());
                }

                // 2. Lưu tên học kỳ để hiển thị
                ViewBag.CurrentSemesterName = $"{activeSemester.SemesterName} - {activeSemester.AcademicYear}";

                // 3. Lấy danh sách lớp CHỈ thuộc học kỳ Active này
                var sections = await _context.CoursesSections
                    .Include(cs => cs.Course)
                    .Where(cs => cs.LecturerId == lecturer.LecturerId && cs.SemesterId == activeSemester.SemesterId)
                    .OrderBy(cs => cs.Course.CourseName) // Sắp xếp cho đẹp
                    .ToListAsync();

                return View("LecturerIndex", sections);
            }

            // --- TRƯỜNG HỢP: SINH VIÊN ---
            if (User.IsInRole("Student"))
            {
                return RedirectToAction(nameof(StudentScore));
            }

            return RedirectToAction("AccessDenied", "Account");
        }

        [Authorize(Roles = "Student")]
            public async Task<IActionResult> StudentScore(int? semesterId)
            {
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null) return RedirectToAction("Login", "Account");

                    var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == currentUser.Id);
                    if (student == null) return View("Error");

                    var semesters = await _context.Semesters
                        .Where(s => s.StartDate.Date >= student.StartStudyDate)
                        .OrderBy(s => s.StartDate)
                        .Select(s => new
                        {
                            Id = s.SemesterId,
                            DisplayText = $"{s.SemesterName} - Năm học {s.AcademicYear}"
                        }).ToListAsync();

                    int selectedValue = semesterId ?? (semesters.FirstOrDefault()?.Id ?? 0);
                    ViewData["SemesterList"] = new SelectList(semesters, "Id", "DisplayText", selectedValue);

                    var currentSemester = semesters.FirstOrDefault(s => s.Id == selectedValue);
                    ViewData["CurrentSemesterName"] = currentSemester?.DisplayText;

                    var scoreList = await _context.Scores
                        .Include(s => s.CourseSection)
                        .ThenInclude(cs => cs.Course)
                        .Where(s => s.StudentId == student.StudentId && s.CourseSection.SemesterId == selectedValue)
                        .ToListAsync();

                    return View(scoreList);
                }
                catch (Exception)
                {
                    return View("Error");
                }
            }

        [Authorize(Roles = "Lecturer")]
        [HttpGet]
        public async Task<IActionResult> EnterGrades(int sectionId)
        {
            var sectionCheck = await _context.CoursesSections
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.CourseSectionId == sectionId);

            if (sectionCheck == null) return NotFound();

            if (!sectionCheck.Semester.IsActive)
            {
                return Content("Lỗi: Không được phép nhập điểm cho học kỳ đã đóng.");
            }

            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Student).ThenInclude(s => s.User)
                .Where(e => e.CourseSectionId == sectionId && e.Status == EnrollmentStatus.Approved)
                .OrderBy(e => e.Student.StudentCode)
                .ToListAsync();

            var existingScores = await _context.Scores
                .Where(s => s.CourseSectionId == sectionId)
                .ToListAsync();

            var modelList = new List<Score>();

            foreach (var enrollment in enrollments)
            {
                var score = existingScores.FirstOrDefault(s => s.StudentId == enrollment.StudentId);

                if (score == null)
                {
                    score = new Score
                    {
                        StudentId = enrollment.StudentId,
                        Student = enrollment.Student,
                        CourseSectionId = sectionId,
                        ProcessScore = 0,
                        MiddleScore = 0,
                        ExamScore = 0
                    };
                }
                else
                {
                    score.Student = enrollment.Student;
                }
                modelList.Add(score);
            }

            ViewBag.SectionId = sectionId;

            return PartialView("_EnterGradesPartial", modelList);
        }


        [Authorize(Roles = "Lecturer")]
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> EnterGrades(List<Score> models, int sectionId)
            {
                var user = await _userManager.GetUserAsync(User);
                var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
                if (lecturer == null) return Forbid();

                if (models != null && models.Count > 0)
                {
                    foreach (var item in models)
                    {
                        // Tính toán
                        float total = (item.ProcessScore * 0.3f) + (item.MiddleScore * 0.2f) + (item.ExamScore * 0.5f);
                        item.FinalScore = (float)Math.Round(total, 2); // Làm tròn 2 chữ số

                        // Xếp loại
                        ScoreValues calculatedGrade;
                        if (total >= 8.5) calculatedGrade = ScoreValues.A;
                        else if (total >= 7.0) calculatedGrade = ScoreValues.B;
                        else if (total >= 5.5) calculatedGrade = ScoreValues.C;
                        else if (total >= 4.0) calculatedGrade = ScoreValues.D;
                        else calculatedGrade = ScoreValues.F;

                        // Cập nhật hoặc Thêm mới
                        var scoreInDb = await _context.Scores.FirstOrDefaultAsync(s => s.StudentId == item.StudentId && s.CourseSectionId == sectionId);

                        if (scoreInDb == null)
                        {
                            // Insert
                            var newScore = new Score
                            {
                                CourseSectionId = sectionId,
                                StudentId = item.StudentId,
                                LecturerId = lecturer.LecturerId,
                                ProcessScore = item.ProcessScore,
                                MiddleScore = item.MiddleScore,
                                ExamScore = item.ExamScore,
                                FinalScore = item.FinalScore,
                                Value = calculatedGrade
                            };
                            _context.Scores.Add(newScore);
                        }
                        else
                        {
                            // Update
                            scoreInDb.ProcessScore = item.ProcessScore;
                            scoreInDb.MiddleScore = item.MiddleScore;
                            scoreInDb.ExamScore = item.ExamScore;
                            scoreInDb.FinalScore = item.FinalScore;
                            scoreInDb.Value = calculatedGrade;
                            scoreInDb.LecturerId = lecturer.LecturerId; // Cập nhật người sửa cuối cùng
                            _context.Scores.Update(scoreInDb);
                        }
                    }
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật bảng điểm thành công!";
                }

                // Sau khi lưu xong, quay lại trang danh sách
                return RedirectToAction(nameof(Index));
            }
        }
    }