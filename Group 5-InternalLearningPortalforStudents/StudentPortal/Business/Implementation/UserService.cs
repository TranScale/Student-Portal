using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Business.Interface;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Business.Implementation
{
    public class UserService : IUserService
    {
        private readonly StudentPortalContext _context;
        private readonly UserManager<User> _userManager;       // Service quản lý User của Identity
        private readonly SignInManager<User> _signInManager;   // Service quản lý Đăng nhập của Identity

        // Inject thêm UserManager và SignInManager vào Constructor
        public UserService(StudentPortalContext context,
                           UserManager<User> userManager,
                           SignInManager<User> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // 1. Authenticate (Đăng nhập)
        public async Task<User?> Authenticate(string username, string password)
        {
            // Identity tự động kiểm tra Username và Hash Password
            // false, false là: không remember me, không khóa tài khoản nếu sai
            var result = await _signInManager.PasswordSignInAsync(username, password, false, false);

            if (result.Succeeded)
            {
                // Nếu đăng nhập thành công, ta truy vấn lại DB để lấy đầy đủ thông tin (kèm Profile)
                // để trả về cho Controller sử dụng
                return await _context.Users
                    .Include(u => u.Student)
                    .Include(u => u.Admin)
                    .Include(u => u.Lecturer)
                    .FirstOrDefaultAsync(u => u.UserName == username);
            }

            return null; // Đăng nhập thất bại
        }

        // 2. Register Student
        public async Task<bool> RegisterStudent(User user, Student student)
        {
            if (string.IsNullOrEmpty(user.Password))
            {
                return false; // Hoặc throw new Exception("Mật khẩu không được để trống");
            }

            var result = await _userManager.CreateAsync(user, user.Password);

            if (result.Succeeded)
            {
                // B2: Gán Role cho User (Dựa vào Enum nhưng chuyển sang String cho Identity)
                await _userManager.AddToRoleAsync(user, "Student");

                // B3: Cập nhật Role Enum trong bảng User (để hiển thị Profile cho dễ)
                user.UserRole = UserRoles.Student;
                await _userManager.UpdateAsync(user);

                // B4: Tạo liên kết Profile Student
                // Lưu ý: user.Id đã được tạo ra sau lệnh CreateAsync ở trên
                student.UserId = user.Id;

                // B5: Lưu Profile vào bảng Students
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return true;
            }

            // Nếu muốn xem lỗi gì (vd: mật khẩu yếu), bạn có thể debug biến 'result.Errors'
            return false;
        }

        // 3. Register Lecturer
        public async Task<bool> RegisterLecturer(User user, Lecturer lecturer)
        {
            if (string.IsNullOrEmpty(user.Password))
            {
                return false; 
            }
            var result = await _userManager.CreateAsync(user, user.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Lecturer");

                user.UserRole = UserRoles.Lecturer;
                await _userManager.UpdateAsync(user);

                lecturer.UserId = user.Id; // Liên kết ID

                _context.Lecturers.Add(lecturer);
                await _context.SaveChangesAsync();

                return true;
            }
            return false;
        }

        // 4. Register Admin
        public async Task<bool> RegisterAdmin(User user, Admin admin)
        {
            if (string.IsNullOrEmpty(user.Password))
            {
                return false; 
            }
            var result = await _userManager.CreateAsync(user, user.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Admin");

                user.UserRole = UserRoles.Admin;
                await _userManager.UpdateAsync(user);

                admin.UserId = user.Id;

                _context.Admins.Add(admin);
                await _context.SaveChangesAsync();

                return true;
            }
            return false;
        }

        // XÓA hàm GetPasswordHash -> Không cần thiết nữa
    }
}