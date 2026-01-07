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
        public async Task<IActionResult> Index(int? semesterId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // --- NẾU LÀ GIẢNG VIÊN ---
            if (User.IsInRole("Lecturer"))
            {
                var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
                if (lecturer == null) return View("Error");

                // 1. Lấy danh sách tất cả các học kỳ (sắp xếp mới nhất lên đầu)
                var semesters = await _context.Semesters
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => new
                    {
                        Id = s.SemesterId,
                        DisplayText = $"{s.SemesterName} - {s.AcademicYear}",
                        IsActive = s.IsActive
                    }).ToListAsync();

                // 2. Xác định học kỳ được chọn
                // Ưu tiên 1: Người dùng chọn (semesterId)
                // Ưu tiên 2: Học kỳ đang Active
                // Ưu tiên 3: Học kỳ mới nhất (đầu danh sách)
                int selectedSemesterId = 0;

                if (semesterId.HasValue)
                {
                    selectedSemesterId = semesterId.Value;
                }
                else
                {
                    var activeSemester = semesters.FirstOrDefault(s => s.IsActive);
                    selectedSemesterId = activeSemester != null ? activeSemester.Id : (semesters.FirstOrDefault()?.Id ?? 0);
                }

                // 3. Tạo Dropdown cho View
                ViewData["SemesterList"] = new SelectList(semesters, "Id", "DisplayText", selectedSemesterId);

                // 4. Lấy tên học kỳ hiện tại để hiển thị tiêu đề
                var currentSemesterObj = semesters.FirstOrDefault(s => s.Id == selectedSemesterId);
                ViewBag.CurrentSemesterName = currentSemesterObj?.DisplayText ?? "Chưa xác định";

                // 5. Lấy danh sách lớp theo Học kỳ đã chọn
                var sections = await _context.CoursesSections
                    .Include(cs => cs.Course)
                    .Where(cs => cs.LecturerId == lecturer.LecturerId && cs.SemesterId == selectedSemesterId)
                    .ToListAsync();

                // Trả về View chính chứa danh sách
                return View("LecturerIndex", sections);
            }

                // --- NẾU LÀ SINH VIÊN (Giữ nguyên logic cũ của bạn) ---
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
                // Lấy danh sách sinh viên đã Approved trong lớp
                var enrollments = await _context.Enrollments
                    .AsNoTracking()
                    .Include(e => e.Student).ThenInclude(s => s.User)
                    .Where(e => e.CourseSectionId == sectionId && e.Status == EnrollmentStatus.Approved)
                    .OrderBy(e => e.Student.StudentCode)
                    .ToListAsync();

                // Lấy điểm đã có (nếu có)
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

                // QUAN TRỌNG: Trả về PartialView để JS load vào Modal
                return PartialView("_EnterGradesPartial", modelList);
            }

            // 3. LƯU ĐIỂM (POST)
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