using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;
using StudentPortal.Models.ViewModels; 

namespace StudentPortal.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được quản lý sinh viên
    public class StudentsController : Controller
    {
        private readonly StudentPortalContext _context;
        private readonly UserManager<User> _userManager;

        public StudentsController(StudentPortalContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. DANH SÁCH SINH VIÊN
        public async Task<IActionResult> Index(string searchString, int? departmentId, int? admissionYear, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentDept"] = departmentId;
            ViewData["CurrentYear"] = admissionYear;

            ViewBag.Departments = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", departmentId);

            var years = await _context.Students
                .Select(s => s.StartStudyDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();
            ViewBag.Years = new SelectList(years, admissionYear);


            var students = _context.Students
                .Include(s => s.User)       
                .Include(s => s.Department)
                .Where(s => !s.IsDeleted)
                .AsNoTracking();            

            if (!string.IsNullOrEmpty(searchString))
            {

                students = students.Where(s =>
                    s.User.FullName.Contains(searchString) ||
                    s.StudentCode.Contains(searchString) ||
                    s.User.Email.Contains(searchString));
            }

            if (departmentId.HasValue)
            {
                students = students.Where(s => s.DepartmentId == departmentId);
            }

            if (admissionYear.HasValue)
            {
                students = students.Where(s => s.StartStudyDate.Year == admissionYear);
            }

            students = students.OrderByDescending(s => s.StartStudyDate);

            int pageSize = 10;
            return View(await PaginatedList<Student>.CreateAsync(students, pageNumber ?? 1, pageSize));
        }

        // 2. TẠO MỚI (GET)
        public IActionResult Create()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName");
            return View();
        }

        // 3. TẠO MỚI (POST) - LOGIC QUAN TRỌNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = model.StudentCode,
                    Email = model.Email,
                    FullName = model.FullName,
                    DateOfBirth = model.DateOfBirth,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    UserRole = UserRoles.Student 
                };

                string password = !string.IsNullOrEmpty(model.Password) ? model.Password : "Student@123";

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Thêm Role "Student" cho User này
                    await _userManager.AddToRoleAsync(user, "Student");

                    // B. TẠO STUDENT (Liên kết với User vừa tạo)
                    var student = new Student
                    {
                        StudentCode = model.StudentCode,
                        DepartmentId = model.DepartmentId,
                        StartStudyDate = DateTime.Today,
                        IsGraduate = false,
                        UserId = user.Id,
                        
                    };

                    _context.Add(student);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Đã thêm sinh viên {model.FullName} thành công!";
                    return RedirectToAction(nameof(Index));
                }

                // Nếu lỗi tạo User (ví dụ trùng Email/User)
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", model.DepartmentId);
            return View(model);
        }

        // 4. EDIT (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.StudentId == id);

            if (student == null) return NotFound();

            // Map dữ liệu từ DB sang ViewModel để hiển thị lên Form
            var model = new StudentViewModel
            {
                StudentId = student.StudentId,
                UserId = student.UserId,
                StudentCode = student.StudentCode,
                DepartmentId = student.DepartmentId,
                StartStudyDate = student.StartStudyDate,
                IsGraduate = student.IsGraduate,
                // Lấy từ bảng User
                FullName = student.User.FullName,
                Email = student.User.Email,
                DateOfBirth = student.User.DateOfBirth,
                Address = student.User.Address,
                PhoneNumber = student.User.PhoneNumber
            };

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", student.DepartmentId);
            return View(model);
        }

        // 5. EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StudentViewModel model)
        {
            if (id != model.StudentId) return NotFound();

            if (ModelState.IsValid)
            {
                // A. Cập nhật bảng STUDENT
                var student = await _context.Students.FindAsync(id);
                if (student == null) return NotFound();

                student.StudentCode = model.StudentCode;
                student.DepartmentId = model.DepartmentId;
                student.StartStudyDate = model.StartStudyDate;
                student.IsGraduate = model.IsGraduate;

                // B. Cập nhật bảng USER
                var user = await _userManager.FindByIdAsync(model.UserId.ToString());
                if (user != null)
                {
                    user.FullName = model.FullName;
                    user.Email = model.Email;
                    user.DateOfBirth = model.DateOfBirth;
                    user.Address = model.Address;
                    user.PhoneNumber = model.PhoneNumber;

                    // Cập nhật User
                    await _userManager.UpdateAsync(user);
                }

                _context.Update(student);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật thông tin thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", model.DepartmentId);
            return View(model);
        }
        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.User)           // Lấy thông tin cá nhân
                .Include(s => s.Department)     // Lấy thông tin Ngành
                .Include(s => s.Enrollments)    // Lấy danh sách đăng ký môn
                    .ThenInclude(e => e.CourseSection) // Lấy tên học phần
                        .ThenInclude(e => e.Course) // Lấy tên môn học
                .FirstOrDefaultAsync(m => m.StudentId == id);

            if (student == null) return NotFound();

            return View(student);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            student.IsDeleted = true;
            _context.Students.Update(student);

            var user = await _userManager.FindByIdAsync(student.UserId.ToString());
            if (user != null)
            {
                user.LockoutEnd = DateTimeOffset.MaxValue; 
                user.LockoutEnabled = true;
                await _userManager.UpdateAsync(user);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã chuyển sinh viên vào danh sách lưu trữ (Đã xóa)!";
            return RedirectToAction(nameof(Index));
        }
    }
}