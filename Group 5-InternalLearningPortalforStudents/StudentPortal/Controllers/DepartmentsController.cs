using System;
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
    [Authorize] // ✅ bắt buộc đăng nhập
    public class DepartmentsController : Controller
    {
        private readonly StudentPortalContext _context;

        public DepartmentsController(StudentPortalContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,Student,Lecturer")]
        // Action Index
        public async Task<IActionResult> Index(string searchString, int? facultyId, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentFaculty"] = facultyId; 

            var departments = _context.Departments.Include(d => d.Faculty).AsQueryable();
            if (facultyId.HasValue)
            {
                departments = departments.Where(d => d.FacultyId == facultyId.Value);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                departments = departments.Where(d => d.DepartmentName.Contains(searchString)
                                                  || d.DepartmentCode.Contains(searchString));
            }

            departments = departments.OrderBy(d => d.DepartmentName);

            ViewBag.Faculties = new SelectList(_context.Faculties, "FacultyId", "FacultyName", facultyId);

            int pageSize = 5;
            return View(await PaginatedList<Department>.CreateAsync(departments.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments
                .Include(d => d.Faculty)
                .Include(d => d.Courses)
                .FirstOrDefaultAsync(m => m.DepartmentId == id);

            if (department == null) return NotFound();

            return View(department);
        }

        // 🔑 Admin mới được tạo
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyCode");
            return View();
        }

        // 🔑 Admin mới được tạo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("DepartmentId,DepartmentName,DepartmentDescription,DepartmentCode,FacultyId")] Department department)
        {
            // ✅ bắt buộc có Faculty hợp lệ
            var facultyOk = await _context.Faculties.AnyAsync(f => f.FacultyId == department.FacultyId);
            if (!facultyOk)
                ModelState.AddModelError("FacultyId", "Faculty không tồn tại. Hãy chọn Faculty hợp lệ.");

            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyCode", department.FacultyId);
            return View(department);
        }

        // 🔑 Admin mới được edit
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound();

            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyCode", department.FacultyId);
            return View(department);
        }

        // 🔑 Admin mới được edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("DepartmentId,DepartmentName,DepartmentDescription,DepartmentCode,FacultyId")] Department department)
        {
            if (id != department.DepartmentId) return NotFound();

            var facultyOk = await _context.Faculties.AnyAsync(f => f.FacultyId == department.FacultyId);
            if (!facultyOk)
                ModelState.AddModelError("FacultyId", "Faculty không tồn tại. Hãy chọn Faculty hợp lệ.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.DepartmentId)) return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyCode", department.FacultyId);
            return View(department);
        }

        // 🔑 Admin mới được delete
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments
                .Include(d => d.Faculty)
                .FirstOrDefaultAsync(m => m.DepartmentId == id);

            if (department == null) return NotFound();

            return View(department);
        }

        // 🔑 Admin mới được delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return RedirectToAction(nameof(Index));

            try
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa Department thành công.";
            }
            catch (DbUpdateException)
            {
                // ✅ tránh crash khi bị FK (đang có Student/Course…)
                TempData["Error"] = "Không thể xóa Department vì đang có dữ liệu liên quan (Student/Course/...). Hãy xóa dữ liệu liên quan trước.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.DepartmentId == id);
        }
    }
}
