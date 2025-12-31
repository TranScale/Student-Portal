using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Models
{
    public class ScheduleItem
    {
        public int ScheduleItemId { get; set; }
        [Required]
        [Display(Name = "Số tuần")]
        public int ScheduleWeek { get; set; }
        [Required]
        [Display(Name = "Ngày học")]
        [DataType(DataType.Date)]
        public DateTime ScheduleDate { get; set; }

        //Khóa ngoại, một thời khóa biểu là một môn học
        [ForeignKey("CourseSection")]
        public int CourseSectionId { get; set; }
        [ValidateNever]
        public CourseSection CourseSection { get; set; } = null!;
    }
}
