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
    [Authorize(Roles = "Admin")] // Chỉ Admin mới vào được
    public class LecturersController : Controller
    {
        private readonly StudentPortalContext _context;
        private readonly UserManager<User> _userManager;

        public LecturersController(StudentPortalContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. DANH SÁCH GIẢNG VIÊN
        public async Task<IActionResult> Index()
        {
            var lecturers = _context.Lecturers
                .Include(l => l.User)
                .Include(l => l.Faculty) // Join bảng Faculty để lấy tên Khoa
                .Where(l => l.User.UserName != "system");
            return View(await lecturers.ToListAsync());
        }

        // 2. GET: CREATE
        public IActionResult Create()
        {
            // Load danh sách Khoa vào Dropdown
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName");
            return View();
        }

        // 3. POST: CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LecturerViewModel model)
        {
            if (ModelState.IsValid)
            {
                // A. TẠO USER (IDENTITY)
                var user = new User
                {
                    UserName = model.LecturerCode, // Mã GV là User Name
                    Email = model.Email,
                    FullName = model.FullName,
                    DateOfBirth = model.DateOfBirth,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    UserRole = UserRoles.Lecturer // Set Enum Role
                };

                // Mật khẩu mặc định: Lecturer@123 (nếu không nhập)
                string password = !string.IsNullOrEmpty(model.Password) ? model.Password : "Lecturer@123";

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Gán quyền "Lecturer"
                    await _userManager.AddToRoleAsync(user, UserRoles.Lecturer.ToString());

                    // B. TẠO LECTURER (Liên kết User + Faculty)
                    var lecturer = new Lecturer
                    {
                        UserId = user.Id,
                        FacultyId = model.FacultyId
                    };

                    _context.Add(lecturer);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Đã thêm giảng viên {model.FullName} thành công!";
                    return RedirectToAction(nameof(Index));
                }

                // Nếu lỗi (trùng User, Email...)
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName", model.FacultyId);
            return View(model);
        }

        // 4. GET: EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var lecturer = await _context.Lecturers
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.LecturerId == id);

            if (lecturer == null) return NotFound();

            // Đổ dữ liệu từ DB lên Form View Model
            var model = new LecturerViewModel
            {
                LecturerId = lecturer.LecturerId,
                UserId = lecturer.UserId,
                LecturerCode = lecturer.User.UserName, // Lấy username làm mã
                FacultyId = lecturer.FacultyId,
                FullName = lecturer.User.FullName,
                Email = lecturer.User.Email,
                DateOfBirth = lecturer.User.DateOfBirth,
                PhoneNumber = lecturer.User.PhoneNumber,
                Address = lecturer.User.Address
            };

            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName", lecturer.FacultyId);
            return View(model);
        }

        // 5. POST: EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LecturerViewModel model)
        {
            if (id != model.LecturerId) return NotFound();

            if (ModelState.IsValid)
            {
                // A. Update Lecturer (Đổi khoa)
                var lecturer = await _context.Lecturers.FindAsync(id);
                if (lecturer == null) return NotFound();

                lecturer.FacultyId = model.FacultyId;
                _context.Update(lecturer);

                // B. Update User Info
                var user = await _userManager.FindByIdAsync(model.UserId.ToString());
                if (user != null)
                {
                    user.FullName = model.FullName;
                    user.Email = model.Email;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Address = model.Address;
                    user.DateOfBirth = model.DateOfBirth;

                    // Lưu ý: Không đổi UserName (Mã GV) ở đây để tránh lỗi hệ thống
                    await _userManager.UpdateAsync(user);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thông tin thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName", model.FacultyId);
            return View(model);
        }
        // GET: Lecturers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var lecturer = await _context.Lecturers
                .Include(l => l.User)
                .Include(l => l.Faculty)
                .Include(l => l.CoursesSection)
                    .ThenInclude(cs => cs.Course)   // Lấy thông tin Môn học (Tên, Mã, Tín chỉ)
                .Include(l => l.CoursesSection)
                    .ThenInclude(cs => cs.Semester) // Lấy thông tin Học kỳ
                .FirstOrDefaultAsync(m => m.LecturerId == id);

            if (lecturer == null) return NotFound();

            return View(lecturer);
        }

        // 6. POST: DELETE
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lecturer = await _context.Lecturers.FindAsync(id);
            if (lecturer == null) return NotFound();

            // Tìm User tương ứng để xóa
            var user = await _userManager.FindByIdAsync(lecturer.UserId.ToString());

            // Xóa Lecturer trước
            _context.Lecturers.Remove(lecturer);

            // Xóa User sau
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa giảng viên và tài khoản liên quan!";
            return RedirectToAction(nameof(Index));
        }
    }
}