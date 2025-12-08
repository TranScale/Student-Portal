using Microsoft.EntityFrameworkCore;
using StudentPortal.Business.Interface;
using StudentPortal.Data;
using StudentPortal.Models;
using System.Security.Cryptography;
using System.Text;

namespace StudentPortal.Business.Implementation
{
    public class UserService : IUserService
    {
       private readonly StudentPortalContext _context;
        public UserService (StudentPortalContext context)
        {
            _context = context;
        }

        //Authenticate
        public async Task<User?> Authenticate(string username, string password)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Admin)
                .Include(u => u.Lecturer)
                .FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null)
                return null;

            var hash = GetPasswordHash(password);
            return user.PasswordHash == hash ? user : null;
        }

        //Register student
        public async Task<bool> RegisterStudent(User user, Student student)
        {
            //Kiểm tra xem có bị trùng UserName hay không
            if(await _context.Users.AnyAsync(u => u.UserName == user.UserName)) 
                return false;

            //Mã hóa mật khẩu
            user.PasswordHash = GetPasswordHash(user.Password);

            //Xác định Role
            user.UserRole = UserRoles.Student;

            //Thêm người dùng(User)
            await _context.AddAsync(user);
            await _context.SaveChangesAsync(); //Lưu để lấy UserId

            //Tạo liên kết giữa Student và User
            student.UserId = user.UserId;
            await _context.Students.AddAsync(student);

            await _context.SaveChangesAsync();
            return true;
        }

        //Register Lecturer
        public async Task<bool> RegisterLecturer(User user, Lecturer lecturer)
        {
            //Kiểm tra xem có bị trùng UserName hay không
            if (await _context.Users.AnyAsync(u => u.UserName == user.UserName))
                return false;

            //Mã hóa mật khẩu
            user.PasswordHash = GetPasswordHash(user.Password);

            //Xác định Role
            user.UserRole = UserRoles.Lecturer;

            //Thêm người dùng(User)
            await _context.AddAsync(user);
            await _context.SaveChangesAsync();  //Lưu để lấy UserId

            //Tạo liên kết giữa Lecturer và User
            lecturer.UserId = user.UserId;
            await _context.AddAsync(lecturer);

            await _context.SaveChangesAsync();

            return true;
        }

        //Register Admin
        public async Task<bool> RegisterAdmin(User user, Admin admin)
        {
            //Kiểm tra xem có bị trùng UserName hay không
            if (await _context.Users.AnyAsync(u => u.UserName == user.UserName))
                return false;

            //Mã hóa mật khẩu
            user.PasswordHash = GetPasswordHash(user.Password);

            //Xác định Role
            user.UserRole = UserRoles.Admin;

            //Thêm người dùng(User)
            await _context.AddAsync(user);
            await _context.SaveChangesAsync();  //Lưu để lấy UserId

            //Tạo liên kết giữa Admin và User
            admin.UserId = user.UserId;
            await _context.AddAsync(admin);

            await _context.SaveChangesAsync();

            return true;
        }

        //Hash password
        public string GetPasswordHash(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
