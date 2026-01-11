
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
    public class CoursesController : Controller
    {
        private readonly StudentPortalContext _context;

        public CoursesController(StudentPortalContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string searchString, int? facultyId, int? departmentId, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentFaculty"] = facultyId;
            ViewData["CurrentDept"] = departmentId;

            var courses = _context.Courses
                .Include(c => c.Department)
                .ThenInclude(d => d.Faculty)
                .AsQueryable();

            if (facultyId.HasValue)
            {
                courses = courses.Where(c => c.Department.FacultyId == facultyId.Value);
            }

            if (departmentId.HasValue)
            {
                courses = courses.Where(c => c.DepartmentId == departmentId.Value);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                courses = courses.Where(c => c.CourseName.Contains(searchString)
                                          || c.CourseCode.Contains(searchString));
            }

            courses = courses.OrderBy(c => c.CourseName);

            ViewBag.Faculties = new SelectList(_context.Faculties, "FacultyId", "FacultyName", facultyId);

            var deptQuery = _context.Departments.AsQueryable();
            if (facultyId.HasValue)
            {
                deptQuery = deptQuery.Where(d => d.FacultyId == facultyId.Value);
            }
            ViewBag.Departments = new SelectList(deptQuery, "DepartmentId", "DepartmentName", departmentId);

            int pageSize = 10; 
            return View(await PaginatedList<Course>.CreateAsync(courses.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.CourseSections)
                .ThenInclude(cs => cs.Enrollments)
                .Include(c => c.CourseSections).ThenInclude(cs => cs.Semester)
                .FirstOrDefaultAsync(m => m.CourseId == id);

            if (course == null) return NotFound();

            return View(course);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("CourseId,CourseName,CourseCode,CourseCredit,DepartmentId")] Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", course.DepartmentId);
            return View(course);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", course.DepartmentId);
            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("CourseId,CourseName,CourseCode,CourseCredit,DepartmentId")] Course course)
        {
            if (id != course.CourseId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.CourseId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", course.DepartmentId);
            return View(course);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Department)
                .FirstOrDefaultAsync(m => m.CourseId == id);

            if (course == null) return NotFound();

            return View(course);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            bool isUsedInSections = await _context.CoursesSections.AnyAsync(cs => cs.CourseId == id);

            if (isUsedInSections)
            {
                TempData["Error"] = $"Không thể xóa môn '{course.CourseName}' vì đang có lớp học phần hoạt động!";
                return RedirectToAction(nameof(Index));
            }
            try
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa môn học thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đã xảy ra lỗi khi xóa.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }
    }
}
