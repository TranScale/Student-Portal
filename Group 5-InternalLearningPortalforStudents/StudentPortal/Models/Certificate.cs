using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Models
{
    public class Certificate
    {
        public int CertificateId { get; set; }

        [Required]
        [StringLength(150)]
        public string CertificateName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CertificateType { get; set; }
        // TOEIC, IELTS, MOS, AWS...

        [StringLength(150)]
        public string? IssuedBy { get; set; }

        [DataType(DataType.Date)]
        public DateTime IssuedDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExpiredDate { get; set; }

        public double? Score { get; set; }

        public string? FilePath { get; set; }


        //Quan hệ người sở hữu
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
