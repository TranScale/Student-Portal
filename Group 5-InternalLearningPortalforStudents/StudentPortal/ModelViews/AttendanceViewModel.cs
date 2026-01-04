using System.ComponentModel.DataAnnotations;
using StudentPortal.Models;

namespace StudentPortal.ModelViews
{
    public class AttendanceViewModel
    {
        // Dữ liệu hiển thị
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string? AvatarPath { get; set; }

        // Dữ liệu để lưu xuống DB
        public int AttendeeId { get; set; } // Nếu = 0 là tạo mới, > 0 là cập nhật
        public int ScheduleItemId { get; set; }
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present; // Mặc định là Có mặt
        public string? Note { get; set; }
    }
}