using StudentPortal.Models;

namespace StudentPortal.Business.Interface
{
    public interface IUserService
    {
        // Xác thực người dùng và trả về thông tin (bao gồm vai trò)
        Task<User?> Authenticate(string username, string password);

        // Đăng ký tài khoản cho sinh viên
        Task<bool> RegisterStudent(User user, Student student);

        // Đăng ký tài khoản cho giảng viên
        Task<bool> RegisterLecturer(User user, Lecturer lecturer);

        // Đăng ký tài khoản cho quản trị viên (chỉ Admin thực hiện)
        Task<bool> RegisterAdmin(User user, Admin admin);

        // Mã hóa mật khẩu trước khi lưu vào DB  
        string GetPasswordHash(string password);
    }
}



