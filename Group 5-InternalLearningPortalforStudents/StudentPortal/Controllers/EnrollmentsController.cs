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
            // TODO: Viết logic lấy danh sách các lớp chưa có giảng viên (LecturerId == null)
            // Để giảng viên chọn và "Đăng ký dạy"
            return Content("Chức năng đăng ký dạy cho Giảng viên đang phát triển...");
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
    }
}