using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Models
{
    public class Attendee
    {
        [Key]
        public int AttendeeId { get; set; }

        // Kết nối với Buổi học cụ thể trong lịch trình
        [Required]
        public int ScheduleItemId { get; set; }

        [ForeignKey("ScheduleItemId")]
        [ValidateNever]
        public ScheduleItem ScheduleItem { get; set; } = null!;

        // Kết nối với Sinh viên (Giả sử bạn đã có class Student)
        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        [ValidateNever]
        public Student Student { get; set; } = null!;

        [Display(Name = "Trạng thái")]
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

        [Display(Name = "Ghi chú")]
        [StringLength(250)]
        public string? Note { get; set; }

        [Display(Name = "Thời điểm điểm danh")]
        public DateTime RecordedAt { get; set; } = DateTime.Now;
    }
}