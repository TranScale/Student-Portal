using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Models
{
    public class Faculty
    {
        public int FacultyId { get; set; }
        [Required]
        [Display(Name = "Tên khoa")]
        public string FacultyName { get; set; } = string.Empty;
        [Display(Name = "Thông tin khoa")]
        public string? FacultyDescription { get; set; }
        [Required]
        [Display(Name = "Mã khoa")]
        public string FacultyCode { get; set; } = string.Empty;

        //Một khoa chứa nhiều ngành
        public List<Department> Departments { get; set; } = new();
    }
}
