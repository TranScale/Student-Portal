using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    [Authorize] // ✅ bắt buộc login trước khi xem bất kỳ action nào
    public class CourseSectionsController : Controller
    {
        private readonly StudentPortalContext _context;

        public CourseSectionsController(StudentPortalContext context)
        {
            _context = context;
        }

        // ✅ User thường + Admin đều xem được
        public async Task<IActionResult> Index()
        {
            var studentPortalContext = _context.CoursesSections
                .Include(c => c.Course)
                .Include(c => c.Lecturer);

            return View(await studentPortalContext.ToListAsync());
        }

        // ✅ User thường + Admin đều xem được
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var courseSection = await _context.CoursesSections
                .Include(c => c.Course)
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(m => m.CourseSectionId == id);

            if (courseSection == null) return NotFound();

            return View(courseSection);
        }

        // ✅ ADMIN mới được CRUD
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseCode");
            ViewData["LecturerId"] = new SelectList(_context.Lecturers, "LecturerId", "LecturerId");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("CourseSectionId,Room,Capacity,Days,DayStart,DayEnd,Sessions,CourseId,LecturerId")] CourseSection courseSection)
        {
            if (ModelState.IsValid)
            {
                _context.Add(courseSection);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseCode", courseSection.CourseId);
            ViewData["LecturerId"] = new SelectList(_context.Lecturers, "LecturerId", "LecturerId", courseSection.LecturerId);
            return View(courseSection);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var courseSection = await _context.CoursesSections.FindAsync(id);
            if (courseSection == null) return NotFound();

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseCode", courseSection.CourseId);
            ViewData["LecturerId"] = new SelectList(_context.Lecturers, "LecturerId", "LecturerId", courseSection.LecturerId);
            return View(courseSection);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("CourseSectionId,Room,Capacity,Days,DayStart,DayEnd,Sessions,CourseId,LecturerId")] CourseSection courseSection)
        {
            if (id != courseSection.CourseSectionId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(courseSection);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseSectionExists(courseSection.CourseSectionId))
                        return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseCode", courseSection.CourseId);
            ViewData["LecturerId"] = new SelectList(_context.Lecturers, "LecturerId", "LecturerId", courseSection.LecturerId);
            return View(courseSection);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var courseSection = await _context.CoursesSections
                .Include(c => c.Course)
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(m => m.CourseSectionId == id);

            if (courseSection == null) return NotFound();

            return View(courseSection);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var courseSection = await _context.CoursesSections.FindAsync(id);
            if (courseSection != null)
            {
                _context.CoursesSections.Remove(courseSection);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CourseSectionExists(int id)
        {
            return _context.CoursesSections.Any(e => e.CourseSectionId == id);
        }
    }
}
