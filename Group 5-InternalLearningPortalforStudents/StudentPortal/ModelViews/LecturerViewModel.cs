using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Models.ViewModels
{
    public class LecturerViewModel
    {
        public int? LecturerId { get; set; } // Dùng cho Edit
        public int? UserId { get; set; }     // Dùng cho Edit

        // --- THÔNG TIN ĐĂNG NHẬP & CÁ NHÂN (Bảng User) ---
        [Required(ErrorMessage = "Vui lòng nhập Mã giảng viên")]
        [Display(Name = "Mã giảng viên (Tên đăng nhập)")]
        public string LecturerCode { get; set; } // Sẽ map vào User.UserName

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Display(Name = "Mật khẩu")]
        public string? Password { get; set; } // Để trống sẽ lấy mật khẩu mặc định

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        // --- THÔNG TIN CÔNG TÁC (Bảng Lecturer) ---
        [Required(ErrorMessage = "Vui lòng chọn Khoa")]
        [Display(Name = "Thuộc Khoa")]
        public int FacultyId { get; set; }
    }
}