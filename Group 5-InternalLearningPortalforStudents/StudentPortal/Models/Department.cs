using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Models
{
    public class Department
    {
        public int DepartmentId { get; set; }
        [Required]
        [Display(Name = "Tên ngành")]
        public string DepartmentName { get; set; } = string.Empty;
        [Display(Name = "Thông tin ngành")]
        public string? DepartmentDescription { get; set; }
        [Display(Name = "Mã ngành")]
        public string DepartmentCode { get; set; } = string.Empty;

        //Khóa ngoại, một khoa chứa nhiều ngành (1..*)
        [ForeignKey("Faculty")]
        public int FacultyId { get; set; }
        [ValidateNever]
        public Faculty Faculty { get; set; } = null!;

        //Một ngành chứa nhiều môn học
        [ValidateNever]
        public List<Course> Courses { get; set; } = new();
    }
}
