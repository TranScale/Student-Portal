using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        [Required]
        [Display(Name = "Mã sinh viên")]
        public string StudentCode { get; set; } = string.Empty;
        [Display(Name = "Trạng thái")]
        public bool IsGraduate { get; set; }

        //Khóa ngoại, sinh viên là một User
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        //Khóa ngoại, sinh viên của một ngành
        [ForeignKey("Department")]
        public int DepartmentId { get; set; }
        [ValidateNever]
        public Department Department { get; set; } = null!;

        //Khóa ngoại, sinh viên có danh sách đăng ký
        [ValidateNever]
        public List<Enrollment> Enrollments { get; set; } = new();
    }
}
