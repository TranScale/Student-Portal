using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Models.ViewModels
{
    public class StudentViewModel
    {
        public int? StudentId { get; set; }
        public int? UserId { get; set; }   

        // --- Thông tin bảng USER ---
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        // --- Thông tin bảng STUDENT ---
        [Required(ErrorMessage = "Vui lòng nhập Mã sinh viên")]
        [Display(Name = "Mã sinh viên")]
        public string StudentCode { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Khoa/Ngành")]
        [Display(Name = "Ngành học")]
        public int DepartmentId { get; set; }

        [Display(Name = "Ngày nhập học")]
        [DataType(DataType.Date)]
        public DateTime StartStudyDate { get; set; } = DateTime.Now;

        [Display(Name = "Đã tốt nghiệp")]
        public bool IsGraduate { get; set; }
        public string? Password { get; set; }
    }
}