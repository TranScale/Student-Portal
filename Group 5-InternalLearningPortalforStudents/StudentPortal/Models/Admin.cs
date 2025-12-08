using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Models
{
    public class Admin
    {
        public int AdminId { get; set; }

        //Khóa ngoại, Một admin là một user
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
