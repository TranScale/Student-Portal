using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Models
{
    public class Lecturer
    {
        public int LecturerId { get; set; }

        //Khóa ngoại, Lecturer là một user
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        //Khóa ngoại, một khoa có nhiều giảng viên
        [ForeignKey("Faculty")]
        public int FacultyId { get; set; }
        [ValidateNever]
        public Faculty Faculty { get; set; } = null!;

        //Khóa ngoại,Một giảng viên dạy nhiều khóa học
        [ValidateNever]
        public List<CourseSection> CoursesSection { get; set; } = new();

    }
}
