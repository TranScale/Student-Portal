using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Models
{
    public class Enrollment
    {
        public int EnrollmentId { get; set; }
        [Display(Name = "Trạng thái")]
        public EnrollmentStatus Status { get; set; }

        //Khóa ngoại, một lượt đăng ký một lớp của một môn học
        [ForeignKey("CourseSection")]
        public int CourseSectionId { get; set; }
        public CourseSection CourseSection { get; set; } = null!;

        //Khóa ngoại, một lượt đăng ký là của một sinh viên
        [ForeignKey("Student")]
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
    }
}
