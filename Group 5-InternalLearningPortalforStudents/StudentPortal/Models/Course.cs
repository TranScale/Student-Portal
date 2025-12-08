using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Models
{
    public class Course
    {
        public int CourseId { get; set; }
        [Required]
        [Display(Name = "Tên khóa học")]
        public string CourseName { get; set; } = string.Empty;
        [Required]
        [Display(Name = "Mã khóa học")]
        public string CourseCode { get; set; } = string.Empty;
        [Required]
        [Display(Name = "Số tín chỉ")]
        public int CourseCredit { get; set; }

        //Khóa ngoại, một ngành có nhiều môn học (1..*)
        [ForeignKey("Department")]
        public int DepartmentId { get; set; }
        [ValidateNever]
        public Department Department { get; set; } = null!;

        //Môn học có nhiều lớp (1..*)
        [ValidateNever]
        public List<CourseSection> CourseSections { get; set; } = new();
        // Môn học có nhiều tài liệu (1..*)
    }
}
