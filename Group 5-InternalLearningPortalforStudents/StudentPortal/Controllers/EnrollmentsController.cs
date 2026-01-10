using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Thêm namespace này để dùng SelectList
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

        public IActionResult Index()
        {
            // 1. Sinh viên -> Đăng ký môn học
            if (User.IsInRole("Student"))
            {
                return RedirectToAction(nameof(StudentEnrollment));
            }

            // 2. Giảng viên -> Đăng ký giờ dạy
            if (User.IsInRole("Lecturer"))
            {
                // Bạn sẽ cần tạo hàm LecturerEnrollment tương tự StudentEnrollment
                return RedirectToAction(nameof(LecturerEnrollment));
            }

            // 3. Admin -> Quản lý danh sách đăng ký
            //if (User.IsInRole("Admin"))
            //{
            //    // Admin dùng Controller khác để quản lý lớp học phần
            //    return RedirectToAction("Index", "CourseSections");
            //}

            return RedirectToAction("AccessDenied", "Account");
        }

        public async Task<IActionResult> StudentEnrollment()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToAction("Login", "Account");

            var activeSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive);
            if (activeSemester == null)
            {
                ViewData["Message"] = "Hiện chưa có học kỳ nào mở đăng ký.";
                return View();
            }

            var registeredList = await _context.Enrollments
                .Include(e => e.CourseSection).ThenInclude(cs => cs.Course)
                .Include(e => e.CourseSection).ThenInclude(cs => cs.Lecturer).ThenInclude(l => l.User)
                .Where(e => e.StudentId == student.StudentId && e.CourseSection.SemesterId == activeSemester.SemesterId)
                .ToListAsync();

            var registeredSectionIds = registeredList.Select(e => e.CourseSectionId).ToList();

            var availableList = await _context.CoursesSections
                .Include(cs => cs.Course)
                .Include(cs => cs.Lecturer).ThenInclude(l => l.User)
                .Include(cs => cs.Enrollments)
                .Where(cs => cs.SemesterId == activeSemester.SemesterId
                          && !registeredSectionIds.Contains(cs.CourseSectionId))
                .ToListAsync();

            ViewData["RegisteredList"] = registeredList;
            ViewData["AvailableList"] = availableList;
            ViewData["SemesterName"] = activeSemester.SemesterName;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRegistration(List<int> selectedCourses)
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToAction("Login", "Account");

            if (selectedCourses == null || selectedCourses.Count == 0)
            {
                TempData["Error"] = "Bạn chưa chọn môn học nào!";
                return RedirectToAction(nameof(StudentEnrollment));
            }

            int successCount = 0;
            int failCount = 0;

            foreach (var sectionId in selectedCourses)
            {
                var section = await _context.CoursesSections
                    .Include(s => s.Enrollments)
                    .FirstOrDefaultAsync(s => s.CourseSectionId == sectionId);

                if (section == null || section.Enrollments.Count >= section.Capacity)
                {
                    failCount++;
                    continue;
                }

                bool exists = await _context.Enrollments
                    .AnyAsync(e => e.StudentId == student.StudentId && e.CourseSectionId == sectionId);

                if (!exists)
                {
                    var enrollment = new Enrollment
                    {
                        StudentId = student.StudentId,
                        CourseSectionId = sectionId,
                        Status = EnrollmentStatus.Pending
                    };
                    _context.Enrollments.Add(enrollment);

                    // Tạo bảng điểm để GV nhập sau này
                    var score = new Score
                    {
                        StudentId = student.StudentId,
                        CourseSectionId = sectionId,
                        LecturerId = section.LecturerId,
                        Value = ScoreValues.F
                    };
                    _context.Scores.Add(score);

                    successCount++;
                }
            }

            await _context.SaveChangesAsync();

            if (successCount > 0) TempData["Success"] = $"Đăng ký thành công {successCount} môn.";
            if (failCount > 0) TempData["Error"] = $"Có {failCount} môn không thể đăng ký do lớp đầy.";

            return RedirectToAction(nameof(StudentEnrollment));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRegistration(int id)
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToAction("Login", "Account");

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.EnrollmentId == id && e.StudentId == student.StudentId);

            if (enrollment != null)
            {
                var score = await _context.Scores
                    .FirstOrDefaultAsync(s => s.StudentId == student.StudentId && s.CourseSectionId == enrollment.CourseSectionId);

                if (score != null) _context.Scores.Remove(score);
                _context.Enrollments.Remove(enrollment);

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã hủy học phần thành công.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy học phần.";
            }

            return RedirectToAction(nameof(StudentEnrollment));
        }

        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> LecturerEnrollment()
        {
            var lecturer = await GetCurrentLecturerAsync();
            if (lecturer == null) return RedirectToAction("Login", "Account");

            var activeSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive);
            if (activeSemester == null)
            {
                ViewData["Message"] = "Hiện chưa có học kỳ nào mở.";
                return View();
            }

            // Lớp CỦA MÌNH (Đã đăng ký) -> Tìm theo ID của mình
            var myClasses = await _context.CoursesSections
                .Include(cs => cs.Course)
                .Where(cs => cs.LecturerId == lecturer.LecturerId && cs.SemesterId == activeSemester.SemesterId)
                .ToListAsync();

            // Lớp CÒN TRỐNG (Available) -> Thay vì tìm null, ta tìm bằng 0
            var availableClasses = await _context.CoursesSections
                .Include(cs => cs.Course)
                // SỬA: So sánh với 0 thay vì null
                .Where(cs => cs.SemesterId == activeSemester.SemesterId && cs.LecturerId == 0)
                .ToListAsync();

            ViewData["MyClasses"] = myClasses;
            ViewData["AvailableClasses"] = availableClasses;
            ViewData["SemesterName"] = activeSemester.SemesterName;

            return View();
        }

        // 2. POST: Đăng ký dạy (Logic giống Student: Chọn -> Kiểm tra -> Lưu)
        [HttpPost]
        [Authorize(Roles = "Lecturer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitTeachingRegistration(List<int> selectedSections)
        {
            var lecturer = await GetCurrentLecturerAsync();
            if (lecturer == null) return RedirectToAction("Login", "Account");

            if (selectedSections == null || selectedSections.Count == 0)
            {
                TempData["Error"] = "Thầy/Cô chưa chọn lớp học phần nào!";
                return RedirectToAction(nameof(LecturerEnrollment));
            }

            int successCount = 0;
            int failCount = 0;

            foreach (var sectionId in selectedSections)
            {
                var section = await _context.CoursesSections.FindAsync(sectionId);

                // SỬA: Kiểm tra xem LecturerId có bằng 0 không (tức là lớp trống)
                if (section != null && section.LecturerId == 0)
                {
                    // Gán lớp này cho giảng viên hiện tại
                    section.LecturerId = lecturer.LecturerId;

                    // Cập nhật các bảng điểm (Score) hiện có của sinh viên trong lớp đó
                    var existingScores = await _context.Scores
                        .Where(s => s.CourseSectionId == sectionId)
                        .ToListAsync();

                    foreach (var score in existingScores)
                    {
                        score.LecturerId = lecturer.LecturerId;
                    }

                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }

            await _context.SaveChangesAsync();

            if (successCount > 0) TempData["Success"] = $"Đăng ký dạy thành công {successCount} lớp.";
            if (failCount > 0) TempData["Error"] = $"{failCount} lớp đã có giảng viên khác đăng ký trước.";

            return RedirectToAction(nameof(LecturerEnrollment));
        }

        // 3. POST: Hủy dạy (Logic giống Student: Tìm -> Xóa liên kết)
        [HttpPost]
        [Authorize(Roles = "Lecturer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelTeaching(int id)
        {
            var lecturer = await GetCurrentLecturerAsync();
            if (lecturer == null) return RedirectToAction("Login", "Account");

            // Tìm lớp mà giảng viên này đang dạy
            var section = await _context.CoursesSections
                .FirstOrDefaultAsync(cs => cs.CourseSectionId == id && cs.LecturerId == lecturer.LecturerId);

            if (section != null)
            {
                // SỬA: Thay vì gán null (gây lỗi), ta gán về 0 (trạng thái trống)
                section.LecturerId = 0;

                // Tìm các điểm số liên quan để gỡ giảng viên ra khỏi điểm số đó
                var existingScores = await _context.Scores
                        .Where(s => s.CourseSectionId == id)
                        .ToListAsync();

                foreach (var score in existingScores)
                {
                    // SỬA: Gán về 0 thay vì null
                    score.LecturerId = 0;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã hủy lớp dạy thành công.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy lớp học phần hoặc lớp không thuộc về bạn.";
            }

            return RedirectToAction(nameof(LecturerEnrollment));
        }

        // Admin: Xem danh sách toàn bộ đăng ký
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> AdminIndex(int? semesterId)
        //{
        //    // Load danh sách học kỳ để lọc
        //    ViewData["SemesterId"] = new SelectList(_context.Semesters, "SemesterId", "SemesterName", semesterId);

        //    var query = _context.Enrollments
        //        .Include(e => e.Student).ThenInclude(s => s.User)
        //        .Include(e => e.CourseSection).ThenInclude(cs => cs.Course)
        //        .Include(e => e.CourseSection).ThenInclude(cs => cs.Semester)
        //        .AsQueryable();

        //    if (semesterId.HasValue)
        //    {
        //        query = query.Where(e => e.CourseSection.SemesterId == semesterId);
        //    }

        //    // Sắp xếp mới nhất lên đầu
        //    var enrollments = await query.OrderByDescending(e => e.EnrollmentId).ToListAsync();
        //    return View(enrollments);
        //}

        //// Admin: GET trang chỉnh sửa trạng thái
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> AdminEdit(int? id)
        //{
        //    if (id == null) return NotFound();

        //    var enrollment = await _context.Enrollments
        //        .Include(e => e.Student).ThenInclude(s => s.User)
        //        .Include(e => e.CourseSection).ThenInclude(c => c.Course)
        //        .Include(e => e.CourseSection).ThenInclude(c => c.Semester)
        //        .FirstOrDefaultAsync(m => m.EnrollmentId == id);

        //    if (enrollment == null) return NotFound();
        //    return View(enrollment);
        //}

        //// Admin: POST cập nhật trạng thái
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> AdminEdit(int id, EnrollmentStatus status)
        //{
        //    var enrollment = await _context.Enrollments.FindAsync(id);
        //    if (enrollment == null) return NotFound();

        //    enrollment.Status = status;
        //    _context.Update(enrollment);
        //    await _context.SaveChangesAsync();

        //    TempData["Success"] = "Cập nhật trạng thái thành công!";
        //    return RedirectToAction(nameof(AdminIndex));
        //}

        //// Admin: Xóa sinh viên khỏi lớp (Bao gồm xóa Score)
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> AdminDelete(int id)
        //{
        //    var enrollment = await _context.Enrollments.FindAsync(id);
        //    if (enrollment != null)
        //    {
        //        // Cần xóa cả bảng điểm (Score) nếu đã được tạo để tránh rác database
        //        var score = await _context.Scores
        //            .FirstOrDefaultAsync(s => s.StudentId == enrollment.StudentId
        //                                   && s.CourseSectionId == enrollment.CourseSectionId);

        //        if (score != null) _context.Scores.Remove(score);

        //        _context.Enrollments.Remove(enrollment);
        //        await _context.SaveChangesAsync();
        //        TempData["Success"] = "Đã xóa sinh viên khỏi lớp học phần.";
        //    }
        //    else
        //    {
        //        TempData["Error"] = "Không tìm thấy dữ liệu để xóa.";
        //    }
        //    return RedirectToAction(nameof(AdminIndex));
        //}

        // Helper
        private async Task<Student> GetCurrentStudentAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
        }

        private async Task<Lecturer> GetCurrentLecturerAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            // Tìm giảng viên có UserId trùng với user đang đăng nhập
            return await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
        }
    }
}