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

            semesters = semesters.OrderByDescending(s => s.IsActive)
                                .ThenByDescending(s => s.StartDate);


            int pageSize = 10;
            return View(await PaginatedList<Semester>.CreateAsync(semesters.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // KÍCH HOẠT HỌC KỲ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCurrent(int id)
        {
            // 1. Tìm học kỳ được chọn
            var semesterToActivate = await _context.Semesters.FindAsync(id);
            if (semesterToActivate == null) return NotFound();

            // 2. Reset tất cả học kỳ khác về False (Inactive)
            var allSemesters = await _context.Semesters.ToListAsync();
            foreach (var sem in allSemesters)
            {
                sem.IsActive = false;
            }

            // 3. Kích hoạt học kỳ được chọn
            semesterToActivate.IsActive = true;

            await _context.SaveChangesAsync();

            // Gửi thông báo nhỏ ra giao diện (nếu bạn dùng TempData trong _Layout)
            TempData["SuccessMessage"] = $"Đã kích hoạt {semesterToActivate.SemesterName} là học kỳ hiện tại.";

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
                // Nếu người dùng tích chọn Active ngay lúc tạo
                if (semester.IsActive)
                {
                    // Tắt hết cái cũ đi
                    var activeSems = await _context.Semesters.Where(s => s.IsActive).ToListAsync();
                    foreach (var s in activeSems) s.IsActive = false;
                }

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
                    // Nếu người dùng tích chọn Active lúc sửa
                    if (semester.IsActive)
                    {
                        var activeSems = await _context.Semesters.Where(s => s.SemesterId != id && s.IsActive).ToListAsync();
                        foreach (var s in activeSems) s.IsActive = false;
                    }

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

        // POST: TỰ ĐỘNG CẬP NHẬT TRẠNG THÁI (Dựa trên ngày hiện tại)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoUpdateCurrentSemester()
        {
            var today = DateTime.Today;

            // 1. Lấy danh sách tất cả học kỳ
            var allSemesters = await _context.Semesters.ToListAsync();

            // 2. Xác định học kỳ Active (Đang diễn ra)
            var activeSemester = allSemesters.FirstOrDefault(s =>
                s.StartDate.Date <= today && s.EndDate.Date >= today);

            bool hasChanges = false;
            int updatedStudents = 0; // Biến đếm số sinh viên được cập nhật

            // --- PHẦN A: CẬP NHẬT TRẠNG THÁI HỌC KỲ (Logic cũ) ---
            foreach (var sem in allSemesters)
            {
                if (activeSemester != null && sem.SemesterId == activeSemester.SemesterId)
                {
                    if (!sem.IsActive) { sem.IsActive = true; hasChanges = true; }
                }
                else
                {
                    if (sem.IsActive) { sem.IsActive = false; hasChanges = true; }
                }
            }

            // --- PHẦN B: CẬP NHẬT TRẠNG THÁI SINH VIÊN (Logic mới) ---

            // 1. XỬ LÝ KHI HỌC KỲ BẮT ĐẦU (Active)
            // Chuyển những sinh viên đang "Chờ lớp/Đăng ký" (Pending) sang "Đã duyệt/Đang học" (Approved)
            if (activeSemester != null)
            {
                var pendingEnrollments = await _context.Enrollments
                    .Where(e => e.CourseSection.SemesterId == activeSemester.SemesterId
                             && e.Status == EnrollmentStatus.Pending) // Chỉ lấy trạng thái Pending
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

            // 2. XỬ LÝ KHI HỌC KỲ KẾT THÚC (Finished)
            // Tìm các học kỳ đã qua ngày kết thúc (EndDate < Today)
            var expiredSemesters = allSemesters
                .Where(s => s.EndDate.Date < today)
                .Select(s => s.SemesterId)
                .ToList();

            if (expiredSemesters.Any())
            {
                // Lấy những sinh viên vẫn đang treo trạng thái "Approved" ở học kỳ cũ
                var finishedEnrollments = await _context.Enrollments
                    .Where(e => expiredSemesters.Contains(e.CourseSection.SemesterId)
                             && e.Status == EnrollmentStatus.Approved) // Chỉ lấy trạng thái Approved cũ
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

            // 3. LƯU DATABASE
            if (hasChanges)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật thành công! Trạng thái học kỳ và {updatedStudents} sinh viên đã được cập nhật.";
            }
            else
            {
                TempData["InfoMessage"] = "Dữ liệu hiện tại đã chính xác, không có thay đổi nào.";
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var semester = await _context.Semesters.FindAsync(id);

            if (semester == null) return RedirectToAction(nameof(Index));

            // 1. KIỂM TRA RÀNG BUỘC DỮ LIỆU
            // Nếu học kỳ đã có lớp học phần -> KHÔNG CHO XÓA
            bool hasCourses = await _context.CoursesSections.AnyAsync(c => c.SemesterId == id);

            if (hasCourses)
            {
                TempData["ErrorMessage"] = $"Không thể xóa {semester.SemesterName} vì đã có lớp học phần được tổ chức trong học kỳ này.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Nếu là học kỳ đang kích hoạt (Active) -> Cảnh báo hoặc Reset
            if (semester.IsActive)
            {
                // Tùy chọn: Có thể cấm xóa học kỳ đang Active
                // Hoặc xóa xong thì không còn học kỳ nào Active nữa
            }

            // 3. Xóa
            _context.Semesters.Remove(semester);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa học kỳ thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}