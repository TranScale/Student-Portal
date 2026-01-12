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

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StudentEnrollment()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToAction("Login", "Account");

            DateTime today = DateTime.Today;

            // 1. Lấy TẤT CẢ các học kỳ đang Active
            var activeSemesters = await _context.Semesters
                .Where(s => s.IsActive)
                .ToListAsync();

            if (!activeSemesters.Any())
            {
                ViewData["Message"] = "Hiện chưa có học kỳ nào được kích hoạt.";
                return View();
            }

            // --- LOGIC MỚI: ƯU TIÊN HỌC KỲ ĐANG MỞ ĐĂNG KÝ ---

            // Tìm trong list active, có ông nào đang trong thời gian cho phép đăng ký không?
            // (Từ OpenDate đến CloseDate)
            var targetSemester = activeSemesters.FirstOrDefault(s =>
                today >= s.StartDate.AddDays(-14) &&
                today <= s.StartDate.AddDays(-7)
            );

            // Nếu KHÔNG có học kỳ nào đang mở cổng, thì lấy học kỳ có ngày bắt đầu mới nhất 
            // (để hiển thị thông báo "Sắp mở" hoặc "Vừa hết hạn" của kỳ đó)
            if (targetSemester == null)
            {
                targetSemester = activeSemesters.OrderByDescending(s => s.StartDate).FirstOrDefault();
            }

            // --- 2. TÍNH TOÁN TRẠNG THÁI KHÓA (Dựa trên targetSemester đã chọn) ---
            DateTime openDate = targetSemester.StartDate.AddDays(-14);
            DateTime closeDate = targetSemester.StartDate.AddDays(-7);

            bool isLocked = true;
            string lockReason = "";

            if (today < openDate)
            {
                isLocked = true;
                lockReason = $"Chưa đến đợt đăng ký {targetSemester.SemesterName}. Cổng sẽ mở từ {openDate:dd/MM/yyyy}.";
            }
            else if (today > closeDate)
            {
                isLocked = true;
                lockReason = $"Đã hết hạn đăng ký {targetSemester.SemesterName} (Hạn chót: {closeDate:dd/MM/yyyy}).";
            }
            else
            {
                isLocked = false; // Đang trong "Khung giờ vàng"
            }

            // Giữ nguyên các Key ViewData để không vỡ giao diện
            ViewData["IsLocked"] = isLocked;
            ViewData["LockReason"] = lockReason;
            ViewData["SemesterName"] = targetSemester.SemesterName;

            // --- 3. LẤY MÔN ĐÃ ĐĂNG KÝ (CHỈ CỦA HỌC KỲ ĐANG ĐƯỢC CHỌN) ---
            var registeredList = await _context.Enrollments
                .Include(e => e.CourseSection).ThenInclude(cs => cs.Course)
                // QUAN TRỌNG: Filter theo targetSemester.SemesterId
                .Where(e => e.StudentId == student.StudentId && e.CourseSection.SemesterId == targetSemester.SemesterId)
                .ToListAsync();

            // --- 4. LẤY MÔN CÓ THỂ ĐĂNG KÝ (CHỈ CỦA HỌC KỲ ĐANG ĐƯỢC CHỌN) ---
            List<CourseSection> availableList = new List<CourseSection>();

            if (!isLocked)
            {
                var registeredSectionIds = registeredList.Select(r => r.CourseSectionId).ToList();

                availableList = await _context.CoursesSections
                    .Include(cs => cs.Course)
                    // QUAN TRỌNG: Filter theo targetSemester.SemesterId
                    .Where(cs => cs.SemesterId == targetSemester.SemesterId
                                 && !registeredSectionIds.Contains(cs.CourseSectionId))
                    .ToListAsync();
            }

            ViewData["RegisteredList"] = registeredList;
            ViewData["AvailableList"] = availableList;

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
            List<string> errorMessages = new List<string>(); // Để lưu chi tiết lỗi

            // 1. Lấy danh sách các môn ĐÃ đăng ký trong học kỳ này để so sánh lịch
            // Lưu ý: Cần lấy danh sách này MỘT LẦN ở ngoài vòng lặp để tối ưu, 
            // nhưng vì học kỳ có thể khác nhau (nếu hệ thống cho phép chọn nhiều kỳ), ta nên lấy theo từng section.
            // Tuy nhiên, logic chuẩn là đăng ký cho "Học kỳ Active".

            var activeSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive);

            // Lấy tất cả lịch đã đăng ký của sinh viên trong học kỳ Active
            var existingEnrollments = await _context.Enrollments
                .Include(e => e.CourseSection).ThenInclude(cs => cs.Course)
                .Where(e => e.StudentId == student.StudentId && e.CourseSection.SemesterId == activeSemester.SemesterId)
                .ToListAsync();

            foreach (var sectionId in selectedCourses)
            {
                var section = await _context.CoursesSections
                    .Include(s => s.Enrollments)
                    .Include(s => s.Course)
                    .FirstOrDefaultAsync(s => s.CourseSectionId == sectionId);

                if (section == null) continue;

                if (section.Enrollments.Count >= section.Capacity)
                {
                    failCount++;
                    errorMessages.Add($"{section.Course.CourseName}: Lớp đã đầy.");
                    continue;
                }

                bool exists = existingEnrollments.Any(e => e.CourseSectionId == sectionId);
                if (exists)
                {
                    failCount++;
                    continue; 
                }
                var conflict = existingEnrollments.FirstOrDefault(e =>
                    (e.CourseSection.Days & section.Days) != 0 &&      
                    (e.CourseSection.Sessions & section.Sessions) != 0 
                );

                if (conflict != null)
                {
                    failCount++;
                    errorMessages.Add($"{section.Course.CourseName}: Trùng lịch với môn {conflict.CourseSection.Course.CourseName} (Phòng {conflict.CourseSection.Room}).");
                    continue;
                }

                // d. Nếu không có lỗi gì -> Tạo đăng ký
                var enrollment = new Enrollment
                {
                    StudentId = student.StudentId,
                    CourseSectionId = sectionId,
                    Status = EnrollmentStatus.Pending
                };
                _context.Enrollments.Add(enrollment);

                var score = new Score
                {
                    StudentId = student.StudentId,
                    CourseSectionId = sectionId,
                    LecturerId = section.LecturerId,
                    Value = ScoreValues.F
                };
                _context.Scores.Add(score);

                existingEnrollments.Add(enrollment);

                successCount++;
            }

            await _context.SaveChangesAsync();

            if (successCount > 0)
                TempData["Success"] = $"Đăng ký thành công {successCount} môn.";

            if (failCount > 0)
            {
                // Hiển thị chi tiết lỗi nếu có
                string errorDetails = string.Join("<br/>", errorMessages);
                TempData["Error"] = $"Không thể đăng ký {failCount} môn.<br/>{errorDetails}";
            }

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

            DateTime today = DateTime.Today;

            // 1. Lấy TẤT CẢ các học kỳ đang Active (Giống bên Sinh viên)
            var activeSemesters = await _context.Semesters
                .Where(s => s.IsActive)
                .ToListAsync();

            if (!activeSemesters.Any())
            {
                ViewData["Message"] = "Hiện chưa có học kỳ nào được kích hoạt.";
                return View();
            }

            // --- LOGIC CHỌN HỌC KỲ: ƯU TIÊN HỌC KỲ ĐANG MỞ ĐĂNG KÝ ---

            // Tìm học kỳ đang trong giai đoạn mở (Trước 14 ngày -> Trước 7 ngày)
            var targetSemester = activeSemesters.FirstOrDefault(s =>
                today >= s.StartDate.AddDays(-14) &&
                today <= s.StartDate.AddDays(-7)
            );

            // Nếu KHÔNG có học kỳ nào đang mở cổng, thì lấy học kỳ có ngày bắt đầu mới nhất 
            // (để hiển thị thông báo "Sắp mở" hoặc "Vừa hết hạn")
            if (targetSemester == null)
            {
                targetSemester = activeSemesters.OrderByDescending(s => s.StartDate).FirstOrDefault();
            }

            // --- 2. TÍNH TOÁN TRẠNG THÁI KHÓA (Dựa trên targetSemester đã chọn) ---
            DateTime openDate = targetSemester.StartDate.AddDays(-14);
            DateTime closeDate = targetSemester.StartDate.AddDays(-7);

            bool isLocked = true;
            string lockReason = "";

            if (today < openDate)
            {
                isLocked = true;
                lockReason = $"Chưa đến đợt đăng ký giảng dạy {targetSemester.SemesterName}. Cổng sẽ mở từ {openDate:dd/MM/yyyy} đến {closeDate:dd/MM/yyyy}.";
            }
            else if (today > closeDate)
            {
                isLocked = true;
                lockReason = $"Đã hết hạn đăng ký giảng dạy {targetSemester.SemesterName} (Hạn chót: {closeDate:dd/MM/yyyy}). Vui lòng liên hệ Phòng Đào Tạo.";
            }
            else
            {
                isLocked = false; // Trong "Tuần lễ vàng"
            }

            // Truyền thông tin trạng thái ra View
            ViewData["IsLocked"] = isLocked;
            ViewData["LockReason"] = lockReason;
            ViewData["SemesterName"] = targetSemester.SemesterName;

            // --- 3. LẤY LỚP ĐÃ ĐĂNG KÝ GIẢNG DẠY (CHỈ CỦA HỌC KỲ ĐANG CHỌN) ---
            var myClasses = await _context.CoursesSections
                .Include(cs => cs.Course)
                // Filter theo targetSemester.SemesterId
                .Where(cs => cs.LecturerId == lecturer.LecturerId && cs.SemesterId == targetSemester.SemesterId)
                .ToListAsync();

            // --- 4. LẤY LỚP CÒN TRỐNG (CHỈ CỦA HỌC KỲ ĐANG CHỌN) ---
            List<CourseSection> availableClasses = new List<CourseSection>();

            if (!isLocked)
            {
                availableClasses = await _context.CoursesSections
                    .Include(cs => cs.Course)
                    // Filter theo targetSemester.SemesterId và LecturerId = 0 (chưa có ai dạy)
                    .Where(cs => cs.SemesterId == targetSemester.SemesterId && cs.LecturerId == 0)
                    .ToListAsync();
            }

            ViewData["MyClasses"] = myClasses;
            ViewData["AvailableClasses"] = availableClasses;

            return View();
        }

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
            List<string> errorMessages = new List<string>();

            var activeSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive);
            if (activeSemester == null)
            {
                TempData["Error"] = "Không tìm thấy học kỳ hiện tại.";
                return RedirectToAction(nameof(LecturerEnrollment));
            }

            var myCurrentSchedule = await _context.CoursesSections
                .Include(cs => cs.Course)
                .Where(cs => cs.LecturerId == lecturer.LecturerId && cs.SemesterId == activeSemester.SemesterId)
                .ToListAsync();

            foreach (var sectionId in selectedSections)
            {
                var section = await _context.CoursesSections
                    .Include(s => s.Course)
                    .FirstOrDefaultAsync(s => s.CourseSectionId == sectionId);

                if (section == null) continue;

                if (section.LecturerId != 0) 
                {
                    failCount++;
                    errorMessages.Add($"Lớp {section.Course.CourseName} ({section.CourseSectionId}) đã có giảng viên khác đăng ký.");
                    continue;
                }

                var conflict = myCurrentSchedule.FirstOrDefault(existing =>
                    (existing.Days & section.Days) != 0 &&       
                    (existing.Sessions & section.Sessions) != 0  
                );

                if (conflict != null)
                {
                    failCount++;
                    errorMessages.Add($"Lớp {section.Course.CourseName} trùng lịch với lớp {conflict.Course.CourseName} (Phòng {conflict.Room}) mà Thầy/Cô đang dạy.");
                    continue;
                }

                section.LecturerId = lecturer.LecturerId;

                var existingScores = await _context.Scores
                    .Where(s => s.CourseSectionId == sectionId)
                    .ToListAsync();

                foreach (var score in existingScores)
                {
                    score.LecturerId = lecturer.LecturerId;
                }

                myCurrentSchedule.Add(section);

                successCount++;
            }

            await _context.SaveChangesAsync();

            if (successCount > 0)
                TempData["Success"] = $"Đăng ký dạy thành công {successCount} lớp.";

            if (failCount > 0)
            {
                string errorDetails = string.Join("<br/>", errorMessages);
                TempData["Error"] = $"Không thể nhận {failCount} lớp:<br/>{errorDetails}";
            }

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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex(int? semesterId)
        {
            // Load danh sách học kỳ để lọc
            ViewData["SemesterId"] = new SelectList(_context.Semesters, "SemesterId", "SemesterName", semesterId);

            var query = _context.Enrollments
                .Include(e => e.Student).ThenInclude(s => s.User)
                .Include(e => e.CourseSection).ThenInclude(cs => cs.Course)
                .Include(e => e.CourseSection).ThenInclude(cs => cs.Semester)
                .AsQueryable();

            if (semesterId.HasValue)
            {
                query = query.Where(e => e.CourseSection.SemesterId == semesterId);
            }

            // Sắp xếp mới nhất lên đầu
            var enrollments = await query.OrderByDescending(e => e.EnrollmentId).ToListAsync();
            return View(enrollments);
        }

        // Admin: GET trang chỉnh sửa trạng thái
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminEdit(int? id)
        {
            if (id == null) return NotFound();

            var enrollment = await _context.Enrollments
                .Include(e => e.Student).ThenInclude(s => s.User)
                .Include(e => e.CourseSection).ThenInclude(c => c.Course)
                .Include(e => e.CourseSection).ThenInclude(c => c.Semester)
                .FirstOrDefaultAsync(m => m.EnrollmentId == id);

            if (enrollment == null) return NotFound();
            return View(enrollment);
        }

        // Admin: POST cập nhật trạng thái
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminEdit(int id, EnrollmentStatus status)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null) return NotFound();

            enrollment.Status = status;
            _context.Update(enrollment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật trạng thái thành công!";
            return RedirectToAction(nameof(AdminIndex));
        }

        // Admin: Xóa sinh viên khỏi lớp (Bao gồm xóa Score)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDelete(int id)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment != null)
            {
                // Cần xóa cả bảng điểm (Score) nếu đã được tạo để tránh rác database
                var score = await _context.Scores
                    .FirstOrDefaultAsync(s => s.StudentId == enrollment.StudentId
                                           && s.CourseSectionId == enrollment.CourseSectionId);

                if (score != null) _context.Scores.Remove(score);

                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa sinh viên khỏi lớp học phần.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy dữ liệu để xóa.";
            }
            return RedirectToAction(nameof(AdminIndex));
        }

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