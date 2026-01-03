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
        public async Task<IActionResult> Index()
        {
            return View(await _context.Faculties.ToListAsync());
        }

        // ✅ Student + Lecturer + Admin đều xem được
        [Authorize(Roles = "Admin,Student,Lecturer")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var faculty = await _context.Faculties
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
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty != null)
            {
                _context.Faculties.Remove(faculty);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool FacultyExists(int id)
        {
            return _context.Faculties.Any(e => e.FacultyId == id);
        }
    }
}
