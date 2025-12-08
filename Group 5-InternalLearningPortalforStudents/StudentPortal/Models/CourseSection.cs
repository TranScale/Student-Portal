using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Models
{
    public class CourseSection
    {
        public int CourseSectionId { get; set; }
        [Required]
        [Display(Name = "Tên phòng")]
        public string Room { get; set; } = string.Empty;
        [Required]
        [Display(Name = "Sĩ số")]
        public int Capacity { get; set; }
        [Required]
        [Display(Name = "Buổi học")]
        public ClassDays Days { get; set; }
        [Required]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime DayStart { get; set; }
        [Required]
        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime DayEnd { get; set; }
        [Required]
        [Display(Name = "Số ca")]
        public StudySessions Sessions { get; set; }

        //Khóa ngoại, một môn học có nhiều lớp
        [ForeignKey("Course")]
        public int CourseId { get; set; }
        [ValidateNever]
        public Course Course { get; set; } = null!;

        //Một lớp của môn học có list đăng ký
        [ValidateNever]
        public List<Enrollment> Enrollments { get; set; } = new();

        //Một khóa học có một người giảng viên
        [ForeignKey("Lecturer")]
        public int LecturerId { get; set; }
        [ValidateNever]
        public Lecturer Lecturer { get; set; } = null!;

        //Một lớp học có các tài liệu
        [ValidateNever]
        public List<CourseMaterial> Materials { get; set; } = new();
    }
}
