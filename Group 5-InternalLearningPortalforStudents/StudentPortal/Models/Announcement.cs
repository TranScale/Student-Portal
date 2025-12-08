using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Models
{
    public class Announcement
    {
        public int AnnouncementId { get; set; }
        [Required]
        [Display(Name = "Tựa đề")]
        public string Title { get; set; } = string.Empty;
        [Required]
        [Display(Name = "Tóm tắt")]
        public string Summary { get; set; } = string.Empty;
        [Required]
        [Display(Name = "Nội dung")]
        public string Content {  get; set; } = string.Empty;
        [Required]
        [Display(Name = "Thời gian tạo")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Hết hạn")]
        [DataType(DataType.DateTime)]
        public DateTime? ExpiredDate { get; set; }
        [Required]
        [Display(Name = "Người nhận")]
        public RecipientType Taker { get; set; }

        //khóa ngoại, người đăng lên 
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
