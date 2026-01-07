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
        public async Task<IActionResult> Index()
        {
            var students = _context.Students
                .Include(s => s.User)       // Join bảng User để lấy Tên
                .Include(s => s.Department); // Join bảng Department để lấy Tên Ngành
            return View(await students.ToListAsync());
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
                // A. TẠO USER (IDENTITY)
                var user = new User
                {
                    UserName = model.StudentCode, // Lấy Mã SV làm tên đăng nhập
                    Email = model.Email,
                    FullName = model.FullName,
                    DateOfBirth = model.DateOfBirth,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    UserRole = UserRoles.Student // Enum của bạn
                };

                // Mật khẩu mặc định: Student@123 (Hoặc lấy từ model.Password)
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
                        StartStudyDate = model.StartStudyDate,
                        IsGraduate = false,
                        UserId = user.Id // <--- LIÊN KẾT KHÓA NGOẠI
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

        // 6. DELETE (Dùng Popup ở Index nên chỉ cần hàm POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Tìm Sinh viên
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            // Tìm User tương ứng để xóa luôn 
            var user = await _userManager.FindByIdAsync(student.UserId.ToString());

            // Xóa Sinh viên trước
            _context.Students.Remove(student);

            // Xóa User sau 
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa hồ sơ sinh viên và tài khoản liên quan!";
            return RedirectToAction(nameof(Index));
        }
    }
}