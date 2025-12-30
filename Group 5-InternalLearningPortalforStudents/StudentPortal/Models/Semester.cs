using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Models
{
    public class Semester
    {
        [Key]
        public int SemesterId { get; set; }

        [Required]
        [Display(Name = "Tên học kỳ")]
        [StringLength(50)]
        public string SemesterName { get; set; } = string.Empty; // Ví dụ: Học kỳ 1 2023-2024

        [Required]
        [Display(Name = "Năm học")]
        public string AcademicYear { get; set; } = string.Empty; // Ví dụ: 2023-2024

        [Required]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; } = false; // Dùng để xác định kỳ hiện tại

        // Mối quan hệ: Một học kỳ có nhiều lớp học (CourseSection)
        [ValidateNever]
        public List<CourseSection> CourseSections { get; set; } = new();
    }
}