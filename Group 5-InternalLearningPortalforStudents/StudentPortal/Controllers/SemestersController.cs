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
            return View(await _context.Semesters.OrderByDescending(s => s.StartDate).ToListAsync());
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
            if (id == null)
            {
                return NotFound();
            }
            var semester = await _context.Semesters
                .FirstOrDefaultAsync(m => m.SemesterId == id);

            if (semester == null)
            {
                return NotFound();
            }

            return View(semester);
        }

        //// GET: Semesters/Delete/5
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null) return NotFound();
        //    var semester = await _context.Semesters.FirstOrDefaultAsync(m => m.SemesterId == id);
        //    if (semester == null) return NotFound();
        //    return View(semester);
        //}

        //// POST: Semesters/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    var semester = await _context.Semesters.FindAsync(id);
        //    if (semester != null)
        //    {
        //        _context.Semesters.Remove(semester);
        //        await _context.SaveChangesAsync();
        //    }
        //    return RedirectToAction(nameof(Index));
        //}
    }
}