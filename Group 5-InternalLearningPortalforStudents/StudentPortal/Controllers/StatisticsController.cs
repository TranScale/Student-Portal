using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;
using System.Dynamic;

namespace StudentPortal.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly StudentPortalContext _context;
        public StatisticsController(StudentPortalContext context) 
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? semesterId, string keyword = "")
        {
            // 1. XỬ LÝ HỌC KỲ
            var semesters = await _context.Semesters.OrderByDescending(s => s.StartDate).ToListAsync();
            var selectedSemester = semesterId.HasValue
                ? semesters.FirstOrDefault(s => s.SemesterId == semesterId.Value)
                : semesters.FirstOrDefault(s => s.IsActive) ?? semesters.FirstOrDefault();

            if (selectedSemester == null) return View();

            int semId = selectedSemester.SemesterId;
            ViewData["Semesters"] = semesters;
            ViewData["SelectedSemesterId"] = semId;
            ViewData["SelectedSemesterName"] = selectedSemester.SemesterName;

            // Lưu lại keyword để hiển thị lại trên View
            ViewData["SearchKeyword"] = keyword;

            // ==========================================================
            // PHẦN A: THỐNG KÊ TỔNG QUAN (METRICS)
            // ==========================================================

            var totalClasses = await _context.CoursesSections.CountAsync(c => c.SemesterId == semId);

            // Đếm sinh viên có đăng ký học phần trong kỳ này
            var totalStudents = await _context.Enrollments
                .Include(e => e.CourseSection)
                .Where(e => e.CourseSection.SemesterId == semId)
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();

            // Đếm giảng viên có dạy trong kỳ này
            var activeLecturers = await _context.CoursesSections
                .Where(c => c.SemesterId == semId)
                .Select(c => c.LecturerId)
                .Distinct()
                .CountAsync();

            // Tính tỷ lệ qua môn (Điểm trung bình >= 4.0 hoặc Score != F)
            var allScores = await _context.Scores
                .Include(s => s.CourseSection)
                .Where(s => s.CourseSection.SemesterId == semId)
                .ToListAsync();

            double passRate = 0;
            if (allScores.Any())
            {
                var passedCount = allScores.Count(s => s.Value != ScoreValues.F);
                passRate = Math.Round((double)passedCount / allScores.Count * 100, 1);
            }

            ViewData["TotalClasses"] = totalClasses;
            ViewData["TotalStudents"] = totalStudents;
            ViewData["ActiveLecturers"] = activeLecturers;
            ViewData["PassRate"] = passRate;

            // ==========================================================
            // PHẦN B: BIỂU ĐỒ TRÒN & TOP SINH VIÊN
            // ==========================================================

            // Phân bố điểm
            var gradeDist = new List<int>
        {
            allScores.Count(s => s.Value == ScoreValues.A),
            allScores.Count(s => s.Value == ScoreValues.B),
            allScores.Count(s => s.Value == ScoreValues.C),
            allScores.Count(s => s.Value == ScoreValues.D),
            allScores.Count(s => s.Value == ScoreValues.F)
        };
            ViewData["GradeDist"] = gradeDist;

            // Top 5 Sinh viên GPA cao nhất
            var topStudents = await _context.Students
                .Where(s => s.Enrollments.Any(e => e.CourseSection.SemesterId == semId))
                .Include(s => s.User)
                .Select(s => new
                {
                    MSSV = s.StudentCode,
                    Name = s.User.FullName,
                    // Tính tạm GPA dựa trên các môn đã có điểm trong kỳ
                    Scores = _context.Scores.Where(sc => sc.StudentId == s.StudentId && sc.CourseSection.SemesterId == semId).ToList()
                })
                .ToListAsync(); // Lấy về RAM để tính toán cho dễ

            var rankedStudents = topStudents.Select(s => new
            {
                s.MSSV,
                s.Name,
                CourseCount = s.Scores.Count,
                GPA = s.Scores.Any() ? s.Scores.Average(x => x.FinalScore) : 0
            })
            .OrderByDescending(x => x.GPA)
            .Take(5)
            .ToList();

            ViewData["TopStudents"] = rankedStudents;

            // ==========================================================
            // PHẦN C: GIẢNG VIÊN & XU HƯỚNG RỚT MÔN
            // ==========================================================

            // Xu hướng rớt môn (So sánh với kỳ trước)
            var prevSem = semesters.FirstOrDefault(s => s.StartDate < selectedSemester.StartDate);
            int failCur = gradeDist[4]; // Số lượng điểm F kỳ này
            int failPrev = 0;

            if (prevSem != null)
            {
                failPrev = await _context.Scores
                   .Include(s => s.CourseSection)
                   .Where(s => s.CourseSection.SemesterId == prevSem.SemesterId && s.Value == ScoreValues.F)
                   .CountAsync();
                ViewData["PrevSemesterName"] = prevSem.SemesterName;
            }
            else
            {
                ViewData["PrevSemesterName"] = "N/A";
            }

            ViewData["FailTrendCurrent"] = failCur;
            ViewData["FailTrendPrev"] = failPrev;

            // Thống kê Giảng viên (Fix lỗi Include Scores)
            // B1: Lấy số lớp dạy
            var lecturerStats = await _context.CoursesSections
                .Where(c => c.SemesterId == semId)
                .Include(c => c.Lecturer).ThenInclude(l => l.User)
                .GroupBy(c => new { c.LecturerId, c.Lecturer.User.FullName })
                .Select(g => new {
                    Id = g.Key.LecturerId,
                    Name = g.Key.FullName,
                    ClassCount = g.Count()
                })
                .ToListAsync();

            // B2: Lấy tỷ lệ đậu của từng GV (Query riêng bảng Score)
            var lecScores = await _context.Scores
                .Where(s => s.CourseSection.SemesterId == semId)
                .GroupBy(s => s.LecturerId)
                .Select(g => new {
                    LecturerId = g.Key,
                    Total = g.Count(),
                    Passed = g.Count(x => x.Value != ScoreValues.F)
                })
                .ToListAsync();

            // B3: Ghép lại
            var topLecturers = lecturerStats.Select(l => {
                var sc = lecScores.FirstOrDefault(x => x.LecturerId == l.Id);
                double rate = (sc != null && sc.Total > 0) ? (double)sc.Passed / sc.Total * 100 : 0;
                return new { Name = l.Name, ClassCount = l.ClassCount, PassRate = Math.Round(rate, 0) };
            })
            .OrderByDescending(x => x.ClassCount)
            .Take(5)
            .ToList();

            ViewData["TopLecturers"] = topLecturers;

            // ==========================================================
            // PHẦN D: XỬ LÝ TÌM KIẾM (SEARCH)
            // ==========================================================
            ViewData["SearchKeyword"] = keyword;
            var results = new List<dynamic>(); // List chứa các đối tượng động

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower().Trim();

                // 1. Tìm Sinh viên
                var students = await _context.Students
                    .Include(s => s.User)
                    .Include(s => s.Department)
                    .Where(s => s.User.FullName.ToLower().Contains(keyword) || s.StudentCode.ToLower().Contains(keyword))
                    .Take(5)
                    .ToListAsync();

                foreach (var s in students)
                {
                    // Dùng ExpandoObject để tạo object động mà View có thể đọc được
                    dynamic item = new ExpandoObject();
                    item.Type = "Sinh viên";
                    item.Code = s.StudentCode;
                    item.Name = s.User.FullName;
                    item.Info = s.Department?.DepartmentName ?? "Chưa phân khoa";
                    item.DetailId = s.StudentId;
                    results.Add(item);
                }

                // 2. Tìm Giảng viên
                var lecturers = await _context.Lecturers
                    .Include(l => l.User)
                    .Include(l => l.Faculty)
                    .Where(l => l.User.FullName.ToLower().Contains(keyword))
                    .Take(5)
                    .ToListAsync();

                foreach (var l in lecturers)
                {
                    dynamic item = new ExpandoObject();
                    item.Type = "Giảng viên";
                    item.Code = "GV" + l.LecturerId; // Giả lập mã nếu không có
                    item.Name = l.User.FullName;
                    item.Info = l.Faculty?.FacultyName ?? "Chưa phân khoa";
                    item.DetailId = l.LecturerId;
                    results.Add(item);
                }
            }

            ViewData["SearchResults"] = results; // Truyền List<dynamic> sang View

            var sectionStats = await _context.Scores
            .Where(s => s.CourseSection.SemesterId == semId)
            .GroupBy(s => s.CourseSectionId)
            .Select(g => new
            {
                SectionId = g.Key,
                TotalStudents = g.Count(),
                FailCount = g.Count(s => s.Value == ScoreValues.F),
                AvgGPA = g.Average(s => s.FinalScore)
            })
            .ToListAsync();

            var riskyStats = sectionStats
                        .Select(s => new
                        {
                            s.SectionId,
                            s.TotalStudents,
                            s.FailCount,
                            FailRate = (double)s.FailCount / s.TotalStudents * 100,
                            s.AvgGPA
                        })
                        // --- [QUAN TRỌNG] THÊM ĐOẠN NÀY ĐỂ LỌC ---
                        // Chỉ lấy lớp có Tỷ lệ rớt >= 40% HOẶC GPA < 5.0
                        .Where(s => s.FailRate >= 40 || s.AvgGPA < 5.0)
                        // ------------------------------------------
                        .OrderByDescending(s => s.FailRate) 
                        .ThenBy(s => s.AvgGPA)            
                        .Take(5)
                        .ToList();

            // 3. Truy vấn lấy thông tin chi tiết (Tên môn, Giảng viên)
            var riskySectionIds = riskyStats.Select(x => x.SectionId).ToList();

            var sectionDetails = await _context.CoursesSections
                .Include(c => c.Course)
                .Include(c => c.Lecturer).ThenInclude(l => l.User)
                .Where(c => riskySectionIds.Contains(c.CourseSectionId))
                .ToListAsync();

            // 4. Ghép dữ liệu để đẩy sang View
            var riskyClasses = new List<dynamic>();
            foreach (var stat in riskyStats)
            {
                var detail = sectionDetails.FirstOrDefault(d => d.CourseSectionId == stat.SectionId);
                if (detail != null)
                {
                    dynamic item = new ExpandoObject();
                    item.CourseName = detail.Course.CourseName; // Tên môn (VD: Lập trình C#)
                    item.LecturerName = detail.Lecturer?.User.FullName ?? "Chưa phân công";
                    item.Total = stat.TotalStudents;
                    item.FailRate = Math.Round(stat.FailRate, 1);
                    item.AvgGPA = Math.Round(stat.AvgGPA, 2);

                    riskyClasses.Add(item);
                }
            }

            ViewData["RiskyClasses"] = riskyClasses;

            return View();
        }
    }
}
