using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // <--- THÊM
using Microsoft.EntityFrameworkCore;
using StudentPortal.Models;

namespace StudentPortal.Data
{
    // 1. Kế thừa IdentityDbContext với 3 tham số: User, Role, Key(int)
    public class StudentPortalContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public StudentPortalContext(DbContextOptions<StudentPortalContext> options)
            : base(options)
        {
        }

        // public DbSet<User> Users { get; set; } 

        // Các bảng khác GIỮ NGUYÊN
        public DbSet<Student> Students { get; set; }
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseSection> CoursesSections { get; set; }
        public DbSet<CourseMaterial> CoursesMaterials { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Score> Scores { get; set; }
        public DbSet<ScheduleItem> ScheduleItems { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<Attendee> Attendees { get; set; }
        public DbSet<Semester> Semesters { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // <--- QUAN TRỌNG: Phải gọi cái này đầu tiên

            // 3. Đổi tên các bảng Identity cho gọn (Tùy chọn)
            modelBuilder.Entity<User>().ToTable("Users"); // Gộp bảng User của bạn vào đây
            modelBuilder.Entity<IdentityRole<int>>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

            // Config khóa ngoại cũ của bạn
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}