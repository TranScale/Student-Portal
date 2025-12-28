using Microsoft.AspNetCore.Identity; // <--- THÊM
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Models
{
    // 1. Kế thừa IdentityUser<int> (Dùng int làm khóa chính)
    public class User : IdentityUser<int>
    {
       
        // public int UserId { get; set; }      
        // public string UserName { get; set; }
        // public string Email { get; set; }    
        // public string PasswordHash { get; set; } 
        // public string PhoneNumber { get; set; } 

        

        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [Display(Name = "Đường dẫn hình ảnh")]
        public string? ImagePath { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }
        [Display(Name = "Thành phố")]
        public string? City { get; set; }
        [Display(Name = "Quốc gia")]
        public string? Country { get; set; }


        [Display(Name = "Mật khẩu")]
        [NotMapped] // Thêm cái này để không tạo cột trong DB, chỉ dùng ở Form
        public string? Password { get; set; }

        // Enum Role của bạn (Vẫn giữ để hiển thị Profile, dù Identity có bảng Role riêng)
        public UserRoles UserRole { get; set; }

        // --- NAVIGATION (GIỮ NGUYÊN) ---
        [ValidateNever]
        public Student? Student { get; set; }
        [ValidateNever]
        public Admin? Admin { get; set; }
        [ValidateNever]
        public Lecturer? Lecturer { get; set; }
        [ValidateNever]
        public List<Announcement>? Announcements { get; set; }
        public List<Certificate>? Certificates { get; set; }
    }
}