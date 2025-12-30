using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Models
{
    public enum AttendanceStatus
    {
        [Display(Name = "Hiện diện")]
        Present,    
        [Display(Name = "Vắng mặt")]
        Absent,     
        [Display(Name = "Đi muộn")]
        Late,       
        [Display(Name = "Vắng có phép")]
        Excused     
    }
}
