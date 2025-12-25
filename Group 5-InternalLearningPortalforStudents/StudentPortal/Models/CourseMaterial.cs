
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Models
{
    public class CourseMaterial
    {
        public int CourseMaterialId { get; set; }
        [Required]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;
        [Required]
        [Display(Name = "Đường dẫn")]
        public string FileUrl { get; set; } = string.Empty;
        [Required]
        [Display(Name = "Dung lượng")]
        public float FileSize { get; set; } //mb
        [Required]
        [Display(Name = "Thời gian đăng")]
        [DataType(DataType.DateTime)]
        public DateTime CreationDate { get; set; }
        [Required]
        [Display(Name = "Trạng thái")]
        public bool IsPublic { get; set; }

        //Khóa ngoại, tài liệu của khóa 
        [ForeignKey("CourseSection")]
        public int CourseSectionId { get; set; }
        [ValidateNever]
        public CourseSection CourseSection { get; set; } = null!;

    }
}