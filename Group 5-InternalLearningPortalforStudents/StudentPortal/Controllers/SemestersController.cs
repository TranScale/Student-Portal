using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> Index()
        {
            // Sắp xếp: Học kỳ đang Active lên đầu, sau đó đến ngày bắt đầu giảm dần
            return View(await _context.Semesters
                .OrderByDescending(s => s.IsActive)
                .ThenByDescending(s => s.StartDate)
                .ToListAsync());
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
    }
}