using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    // ✅ Bắt buộc đăng nhập cho tất cả action
    [Authorize]
    public class FacultiesController : Controller
    {
        private readonly StudentPortalContext _context;

        public FacultiesController(StudentPortalContext context)
        {
            _context = context;
        }

        // ✅ Student + Lecturer + Admin đều xem được
        [Authorize(Roles = "Admin,Student,Lecturer")]
        // Nhớ thêm using StudentPortal.Models; (hoặc namespace chứa PaginatedList)

        public async Task<IActionResult> Index(string searchString, string sortOrder, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentFilter"] = searchString;

            var faculties = _context.Faculties.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                faculties = faculties.Where(f => f.FacultyName.Contains(searchString)
                                              || f.FacultyCode.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    faculties = faculties.OrderByDescending(f => f.FacultyName);
                    break;
                default:
                    faculties = faculties.OrderBy(f => f.FacultyName);
                    break;
            }

            int pageSize = 5; 
            return View(await PaginatedList<StudentPortal.Models.Faculty>.CreateAsync(faculties.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // ✅ Student + Lecturer + Admin đều xem được
        [Authorize(Roles = "Admin,Student,Lecturer")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var faculty = await _context.Faculties
                .Include(f => f.Departments)
                .FirstOrDefaultAsync(m => m.FacultyId == id);

            if (faculty == null) return NotFound();

            return View(faculty);
        }

        // 🔑 Admin mới được tạo
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // 🔑 Admin mới được tạo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("FacultyId,FacultyName,FacultyDescription,FacultyCode")] Faculty faculty)
        {
            if (ModelState.IsValid)
            {
                _context.Add(faculty);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(faculty);
        }

        // 🔑 Admin mới được edit
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null) return NotFound();

            return View(faculty);
        }

        // 🔑 Admin mới được edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("FacultyId,FacultyName,FacultyDescription,FacultyCode")] Faculty faculty)
        {
            if (id != faculty.FacultyId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(faculty);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FacultyExists(faculty.FacultyId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(faculty);
        }

        // 🔑 Admin mới được delete
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var faculty = await _context.Faculties
                .FirstOrDefaultAsync(m => m.FacultyId == id);

            if (faculty == null) return NotFound();

            return View(faculty);
        }

        // 🔑 Admin mới được delete
        // Trong FacultiesController.cs

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // 1. Kiểm tra xem có Ngành (Departments) nào đang thuộc Khoa này không
            var hasDepartments = await _context.Departments.AnyAsync(d => d.FacultyId == id);

            if (hasDepartments)
            {
                // Nếu còn dính dữ liệu con, báo lỗi và không xóa
                TempData["Error"] = "Không thể xóa Khoa này vì đang có các Ngành trực thuộc. Vui lòng xóa các Ngành trước.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Tìm và xóa Khoa
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty != null)
            {
                try
                {
                    _context.Faculties.Remove(faculty);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Xóa Khoa thành công!";
                }
                catch (DbUpdateException)
                {
                    // Bắt lỗi nếu còn sót các bảng khác (ví dụ bảng Students nếu có liên kết trực tiếp)
                    TempData["Error"] = "Lỗi hệ thống: Không thể xóa do ràng buộc dữ liệu.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool FacultyExists(int id)
        {
            return _context.Faculties.Any(e => e.FacultyId == id);
        }
    }
}
