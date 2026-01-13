using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SemestersController : Controller
    {
        private readonly StudentPortalContext _context;

        public SemestersController(StudentPortalContext context)
        {
            _context = context;
        }

        // GET: Semesters
        public async Task<IActionResult> Index(string academicYear, int? pageNumber)
        {
            var years = await _context.Semesters
                .Select(s => s.AcademicYear)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            ViewBag.AcademicYears = new SelectList(years, academicYear);
            ViewData["CurrentYear"] = academicYear;

            var semesters = _context.Semesters.AsQueryable();

            if (!string.IsNullOrEmpty(academicYear))
            {
                semesters = semesters.Where(s => s.AcademicYear == academicYear);
            }

            // Sắp xếp: Active lên đầu, sau đó đến ngày bắt đầu giảm dần
            semesters = semesters.OrderByDescending(s => s.IsActive)
                               .ThenByDescending(s => s.StartDate);

            int pageSize = 10;
            return View(await PaginatedList<Semester>.CreateAsync(semesters.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // --- [NEW] HÀM BẬT/TẮT TRẠNG THÁI (TOGGLE) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null) return NotFound();

            // Đảo ngược trạng thái: True -> False, False -> True
            semester.IsActive = !semester.IsActive;

            await _context.SaveChangesAsync();

            string statusMsg = semester.IsActive ? "được kích hoạt" : "đã dừng kích hoạt";
            TempData["SuccessMessage"] = $"Học kỳ {semester.SemesterName} {statusMsg}.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Semesters/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Semesters/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Semester semester)
        {
            if (ModelState.IsValid)
            {
                // [ĐÃ SỬA] Không còn tắt các học kỳ khác khi tạo mới Active
                _context.Add(semester);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(semester);
        }

        // GET: Semesters/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null) return NotFound();
            return View(semester);
        }

        // POST: Semesters/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Semester semester)
        {
            if (id != semester.SemesterId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // [ĐÃ SỬA] Không còn tắt các học kỳ khác khi Edit Active
                    _context.Update(semester);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Semesters.Any(e => e.SemesterId == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(semester);
        }

        // GET: Semesters/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var semester = await _context.Semesters.FirstOrDefaultAsync(m => m.SemesterId == id);
            if (semester == null) return NotFound();
            return View(semester);
        }

        // POST: TỰ ĐỘNG CẬP NHẬT TRẠNG THÁI (Theo ngày)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoUpdateCurrentSemester()
        {
            var today = DateTime.Today;
            var allSemesters = await _context.Semesters.ToListAsync();

            // [ĐÃ SỬA] Tìm TẤT CẢ các học kỳ thỏa mãn ngày hiện tại (thay vì chỉ 1)
            var activeSemesters = allSemesters.Where(s =>
                s.StartDate.Date <= today && s.EndDate.Date >= today).ToList();

            bool hasChanges = false;
            int updatedStudents = 0;

            // 1. Cập nhật trạng thái Active cho học kỳ
            foreach (var sem in allSemesters)
            {
                // Kiểm tra xem học kỳ này có nằm trong danh sách cần Active không
                bool shouldBeActive = activeSemesters.Any(s => s.SemesterId == sem.SemesterId);

                if (sem.IsActive != shouldBeActive)
                {
                    sem.IsActive = shouldBeActive;
                    hasChanges = true;
                }
            }

            // 2. Logic cập nhật sinh viên (Enrollment) giữ nguyên, 
            // nhưng áp dụng cho TẤT CẢ activeSemesters
            if (activeSemesters.Any())
            {
                var activeIds = activeSemesters.Select(s => s.SemesterId).ToList();

                var pendingEnrollments = await _context.Enrollments
                    .Where(e => activeIds.Contains(e.CourseSection.SemesterId)
                             && e.Status == EnrollmentStatus.Pending)
                    .ToListAsync();

                if (pendingEnrollments.Any())
                {
                    foreach (var enrollment in pendingEnrollments)
                    {
                        enrollment.Status = EnrollmentStatus.Approved;
                    }
                    updatedStudents += pendingEnrollments.Count;
                    hasChanges = true;
                }
            }

            // 3. Xử lý học kỳ kết thúc (Finished)
            var expiredSemesters = allSemesters
                .Where(s => s.EndDate.Date < today)
                .Select(s => s.SemesterId)
                .ToList();

            if (expiredSemesters.Any())
            {
                var finishedEnrollments = await _context.Enrollments
                    .Where(e => expiredSemesters.Contains(e.CourseSection.SemesterId)
                             && e.Status == EnrollmentStatus.Approved)
                    .ToListAsync();

                if (finishedEnrollments.Any())
                {
                    foreach (var enrollment in finishedEnrollments)
                    {
                        enrollment.Status = EnrollmentStatus.Finished;
                    }
                    updatedStudents += finishedEnrollments.Count;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật tự động thành công! ({updatedStudents} sinh viên được cập nhật).";
            }
            else
            {
                TempData["InfoMessage"] = "Dữ liệu đã đồng bộ theo ngày, không có thay đổi nào.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null) return RedirectToAction(nameof(Index));

            bool hasCourses = await _context.CoursesSections.AnyAsync(c => c.SemesterId == id);
            if (hasCourses)
            {
                TempData["ErrorMessage"] = $"Không thể xóa {semester.SemesterName} vì đã có lớp học phần.";
                return RedirectToAction(nameof(Index));
            }

            _context.Semesters.Remove(semester);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa học kỳ thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finish(int id)
        {
            // 1. Tìm học kỳ
            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null) return NotFound();

            // 2. Tắt trạng thái Active
            semester.IsActive = false;

            // 3. Tìm tất cả Enrollment đã được duyệt (Approved) thuộc học kỳ này
            // Logic: Enrollment -> CourseSection -> SemesterId
            // Lưu ý: Chỉ chuyển những người ĐANG HỌC (Approved) thành ĐÃ HỌC (Finished).
            // Không đụng vào Pending (chưa được duyệt) hoặc Cancelled (đã hủy).
            var approvedEnrollments = await _context.Enrollments
                .Where(e => e.CourseSection.SemesterId == id
                         && e.Status == EnrollmentStatus.Approved)
                .ToListAsync();

            // 4. Cập nhật trạng thái sang Finished
            foreach (var enrollment in approvedEnrollments)
            {
                enrollment.Status = EnrollmentStatus.Finished;
            }

            // 5. Lưu thay đổi
            await _context.SaveChangesAsync();

            // 6. Thông báo kết quả
            TempData["SuccessMessage"] = $"Đã kết thúc học kỳ {semester.SemesterName} thành công. " +
                                         $"{approvedEnrollments.Count} sinh viên đã được cập nhật trạng thái Hoàn thành.";

            return RedirectToAction(nameof(Index));
        }
    }
}