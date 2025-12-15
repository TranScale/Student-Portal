using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Models
{
    public class Score
    {
        public int ScoreId { get; set; }
        [Display(Name = "Điểm")]
        public ScoreValues Value { get; set; }

        [Display(Name = "Điểm quá trình")]
        public float ProcessScore { get; set; } //30%
        [Display(Name = "Điểm giữa kì")]
        public float MiddleScore { get; set; } //20%
        [Display(Name = "Điểm thi")]
        public float ExamScore { get; set; } // 50%

        //Khóa ngoại, điểm của một môn học
        [ForeignKey("CourseSection")]
        public int CourseSectionId { get; set; }
        [ValidateNever]
        public CourseSection CourseSection { get; set; } = null!;

        //Khóa ngoại, điểm một học của một sinh viên
        [ForeignKey("Student")]
        public int StudentId { get; set; }
        [ValidateNever]
        public Student Student { get; set; } = null!;

        //Khóa ngoại, người chấm điểm
        [ForeignKey("Lecturer")]
        public int LecturerId { get; set; }
        [ValidateNever]
        public Lecturer Lecturer { get; set; } = null!;

    }
}
