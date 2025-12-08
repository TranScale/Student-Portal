using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Models
{
    public class User
    {
        public int UserId {  get; set; }
        [Display(Name = "Đường dẫn hình ảnh")]
        public string? ImagePath { get; set; } //Link dẫn đường ảnh
        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; } //Địa chỉ, đường, quận,phường
        [Display(Name = "Thành phố")]
        public string? City { get; set; } // Thành phố
        [Display(Name = "Quốc gia")]
        public string? Country { get; set; } // Quốc gia

        [Display(Name = "Tên người dùng")]
        public string UserName { get; set; } = string.Empty;
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // Mã hash của password

        public UserRoles UserRole { get; set; } // Student, lecturer, admin

        //Navigation 
        [ValidateNever]
        public Student? Student { get; set; }
        [ValidateNever]
        public Admin? Admin { get; set; }
        [ValidateNever]
        public Lecturer? Lecturer { get; set; }
        [ValidateNever]
        public List<Announcement>? Announcements { get; set; }


    }
}
