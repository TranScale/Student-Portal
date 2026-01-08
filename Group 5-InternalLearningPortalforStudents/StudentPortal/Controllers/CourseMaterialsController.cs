using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentPortal.Controllers
{
    public class CourseMaterialsController : Controller
    {
        private readonly StudentPortalContext _context;
        private readonly UserManager<User> _userManager;

        public CourseMaterialsController(StudentPortalContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index ()
        {
            if(User.IsInRole("Student"))
            {
                return RedirectToAction(nameof(StudentIndex));
            }
            return View();
        }

        // GET: CourseMaterials
        // Thêm 2 tham số: searchString (từ khóa tìm kiếm) và sortOrder (kiểu sắp xếp)
        public async Task<IActionResult> StudentIndex(string searchString, string sortOrder)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var currentStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (currentStudent == null) return NotFound();

            // 1. Tạo câu truy vấn (chưa chạy xuống DB ngay, để còn filter tiếp)
            var enrollmentsQuery = _context.Enrollments
                .Include(e => e.CourseSection)
                .ThenInclude(cs => cs.Course)
                .Where(e => e.StudentId == currentStudent.StudentId)
                .AsQueryable(); // Chuyển sang Queryable để nối thêm điều kiện

            // 2. Xử lý TÌM KIẾM (Nếu có từ khóa)
            if (!string.IsNullOrEmpty(searchString))
            {
                // Tìm theo Tên môn học HOẶC Mã môn học
                enrollmentsQuery = enrollmentsQuery.Where(e =>
                    e.CourseSection.Course.CourseName.Contains(searchString) ||
                    e.CourseSection.Course.CourseCode.Contains(searchString));
            }

            // 3. Xử lý SẮP XẾP
            // Lưu trạng thái sắp xếp để View biết đang sort kiểu gì
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";

            switch (sortOrder)
            {
                case "name_desc": // Z-A
                    enrollmentsQuery = enrollmentsQuery.OrderByDescending(e => e.CourseSection.Course.CourseName);
                    break;
                default: // Mặc định là A-Z
                    enrollmentsQuery = enrollmentsQuery.OrderBy(e => e.CourseSection.Course.CourseName);
                    break;
            }

            // 4. Thực thi truy vấn và lấy dữ liệu
            var enrollments = await enrollmentsQuery.ToListAsync();

            // Lưu lại từ khóa tìm kiếm để hiển thị lại trên ô input
            ViewData["CurrentFilter"] = searchString;
            ViewData["Courses"] = enrollments;

            return View();
        }
        // Trong CourseMaterialsController
        public async Task<IActionResult> Details(int? id, string searchString)
        {
            if (id == null) return NotFound();

            // 1. Lấy thông tin Lớp học phần (để hiện tên trên header)
            var courseSection = await _context.CoursesSections
                .Include(cs => cs.Course)
                .FirstOrDefaultAsync(m => m.CourseSectionId == id);

            if (courseSection == null) return NotFound();

            // 2. Query lấy danh sách FILE (CourseMaterial)
            // Chỉ lấy file IsPublic = true
            var materialsQuery = _context.CoursesMaterials
                .Where(m => m.CourseSectionId == id && m.IsPublic == true)
                .OrderByDescending(m => m.CreationDate) // Mới nhất lên đầu
                .AsQueryable();

            // 3. Xử lý tìm kiếm (Theo Title)
            if (!string.IsNullOrEmpty(searchString))
            {
                materialsQuery = materialsQuery.Where(s => s.Title.Contains(searchString));
            }

            var materials = await materialsQuery.ToListAsync();

            // 4. Nếu là AJAX (gõ tìm kiếm) -> Trả về PartialView bảng
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_MaterialTable", materials);
            }

            // 5. Trả về View chính
            ViewData["CourseName"] = $"[{courseSection.Course.CourseCode}] {courseSection.Course.CourseName}";
            ViewData["SectionId"] = id; // Lưu ID để giữ context khi search
            ViewData["CurrentFilter"] = searchString;

            return View(materials);
        }

        // GET: CourseMaterials/Create
        public IActionResult Create()
        {
            ViewData["CourseSectionId"] = new SelectList(_context.CoursesSections, "CourseSectionId", "Room");
            return View();
        }

        // POST: CourseMaterials/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseMaterialId,Title,FileUrl,FileSize,CreationDate,IsPublic,CourseSectionId")] CourseMaterial courseMaterial)
        {
            if (ModelState.IsValid)
            {
                _context.Add(courseMaterial);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseSectionId"] = new SelectList(_context.CoursesSections, "CourseSectionId", "Room", courseMaterial.CourseSectionId);
            return View(courseMaterial);
        }

        // GET: CourseMaterials/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var courseMaterial = await _context.CoursesMaterials.FindAsync(id);
            if (courseMaterial == null)
            {
                return NotFound();
            }
            ViewData["CourseSectionId"] = new SelectList(_context.CoursesSections, "CourseSectionId", "Room", courseMaterial.CourseSectionId);
            return View(courseMaterial);
        }

        // POST: CourseMaterials/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CourseMaterialId,Title,FileUrl,FileSize,CreationDate,IsPublic,CourseSectionId")] CourseMaterial courseMaterial)
        {
            if (id != courseMaterial.CourseMaterialId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(courseMaterial);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseMaterialExists(courseMaterial.CourseMaterialId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseSectionId"] = new SelectList(_context.CoursesSections, "CourseSectionId", "Room", courseMaterial.CourseSectionId);
            return View(courseMaterial);
        }

        // GET: CourseMaterials/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var courseMaterial = await _context.CoursesMaterials
                .Include(c => c.CourseSection)
                .FirstOrDefaultAsync(m => m.CourseMaterialId == id);
            if (courseMaterial == null)
            {
                return NotFound();
            }

            return View(courseMaterial);
        }

        // POST: CourseMaterials/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var courseMaterial = await _context.CoursesMaterials.FindAsync(id);
            if (courseMaterial != null)
            {
                _context.CoursesMaterials.Remove(courseMaterial);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseMaterialExists(int id)
        {
            return _context.CoursesMaterials.Any(e => e.CourseMaterialId == id);
        }
    }
}
