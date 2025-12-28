using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StudentPortal.Data
{
    public static class DbInitSeed
    {
        // Thêm tham số UserManager và RoleManager
        public static async Task InitializeAsync(StudentPortalContext context,
                                                 UserManager<User> userManager,
                                                 RoleManager<IdentityRole<int>> roleManager)
        {
            // 1. Tạo database nếu chưa có
            await context.Database.EnsureCreatedAsync();

            // 2. Kiểm tra dữ liệu cũ
            if (context.Faculties.Any())
            {
                return; // Đã có dữ liệu thì dừng
            }

            // ==========================================
            // PHẦN 1: DỮ LIỆU DANH MỤC (KHOA, NGÀNH, MÔN)
            // ==========================================

            // A. Tạo Khoa
            var faculties = new Faculty[]
            {
                new Faculty { FacultyName = "Công nghệ thông tin", FacultyCode = "CNTT", FacultyDescription = "Khoa đào tạo CNTT" },
                new Faculty { FacultyName = "Kinh tế", FacultyCode = "KT", FacultyDescription = "Khoa Kinh tế và Quản lý" }
            };
            context.Faculties.AddRange(faculties);
            await context.SaveChangesAsync();

            // B. Tạo Ngành
            var cntt = await context.Faculties.FirstAsync(f => f.FacultyCode == "CNTT");
            var kt = await context.Faculties.FirstAsync(f => f.FacultyCode == "KT");

            var departments = new Department[]
            {
                new Department { DepartmentName = "Kỹ thuật phần mềm", DepartmentCode = "SE", FacultyId = cntt.FacultyId },
                new Department { DepartmentName = "Hệ thống thông tin", DepartmentCode = "IS", FacultyId = cntt.FacultyId },
                new Department { DepartmentName = "Quản trị kinh doanh", DepartmentCode = "BA", FacultyId = kt.FacultyId }
            };
            context.Departments.AddRange(departments);
            await context.SaveChangesAsync();

            // C. Tạo Môn học
            var se = await context.Departments.FirstAsync(d => d.DepartmentCode == "SE");
            var ba = await context.Departments.FirstAsync(d => d.DepartmentCode == "BA");

            var courses = new Course[]
            {
                new Course { CourseName = "Lập trình C# căn bản", CourseCode = "PRN211", CourseCredit = 3, DepartmentId = se.DepartmentId },
                new Course { CourseName = "Cấu trúc dữ liệu", CourseCode = "CSD201", CourseCredit = 3, DepartmentId = se.DepartmentId },
                new Course { CourseName = "Kinh tế vi mô", CourseCode = "ECO101", CourseCredit = 3, DepartmentId = ba.DepartmentId }
            };
            context.Courses.AddRange(courses);
            await context.SaveChangesAsync();


            // ==========================================
            // PHẦN 2: TẠO USER & ROLE (IDENTITY)
            // ==========================================

            // A. Tạo Role trước (Admin, Lecturer, Student)
            string[] roleNames = { "Admin", "Lecturer", "Student" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                }
            }

            // B. Hàm tạo User (Local Function để tái sử dụng code)
            async Task CreateUserWithRole(User user, string password, string role)
            {
                // Identity tự check trùng user/email và tự Hash password
                var result = await userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Gán Role
                    await userManager.AddToRoleAsync(user, role);
                }
            }

            // C. Định nghĩa và tạo các User
            var passwordChung = "Student@123"; // Password phải có Hoa, thường, số, ký tự đặc biệt

            // 1. Admin
            var adminUser = new User
            {
                UserName = "admin",
                Email = "admin@portal.com",
                FullName = "Quản trị viên",
                UserRole = UserRoles.Admin, // Enum của bạn
                DateOfBirth = DateTime.Parse("1990-01-01"),
                PhoneNumber = "0909000111",
                Address = "HCM",
                City = "HCM",
                Country = "Vietnam",
                EmailConfirmed = true
            };
            await CreateUserWithRole(adminUser, passwordChung, "Admin");

            // 2. Lecturer
            var lecturerUser = new User
            {
                UserName = "gv01",
                Email = "giangnv@portal.com",
                FullName = "Nguyễn Văn Giảng",
                UserRole = UserRoles.Lecturer,
                DateOfBirth = DateTime.Parse("1985-05-15"),
                EmailConfirmed = true
            };
            await CreateUserWithRole(lecturerUser, passwordChung, "Lecturer");

            // 3. Student 1
            var studentUser1 = new User
            {
                UserName = "sv01",
                Email = "troth@portal.com",
                FullName = "Trần Học Trò",
                UserRole = UserRoles.Student,
                DateOfBirth = DateTime.Parse("2003-08-20"),
                EmailConfirmed = true
            };
            await CreateUserWithRole(studentUser1, passwordChung, "Student");

            // 4. Student 2
            var studentUser2 = new User
            {
                UserName = "sv02",
                Email = "buoilt@portal.com",
                FullName = "Lê Thị Bưởi",
                UserRole = UserRoles.Student,
                DateOfBirth = DateTime.Parse("2003-09-10"),
                EmailConfirmed = true
            };
            await CreateUserWithRole(studentUser2, passwordChung, "Student");


            // ==========================================
            // PHẦN 3: TẠO PROFILE CHI TIẾT (ADMIN, LECTURER, STUDENT)
            // ==========================================
            // Lưu ý: Lúc này các User trên đã có Id (int) do Identity sinh ra.

            // 1. Admin Profile
            if (await context.Users.AnyAsync(u => u.UserName == "admin"))
            {
                var user = await context.Users.FirstAsync(u => u.UserName == "admin");
                context.Admins.Add(new Admin { UserId = user.Id }); // Dùng user.Id
            }

            // 2. Lecturer Profile
            if (await context.Users.AnyAsync(u => u.UserName == "gv01"))
            {
                var user = await context.Users.FirstAsync(u => u.UserName == "gv01");
                context.Lecturers.Add(new Lecturer { UserId = user.Id, FacultyId = cntt.FacultyId });
            }

            // 3. Student Profiles
            if (await context.Users.AnyAsync(u => u.UserName == "sv01"))
            {
                var user = await context.Users.FirstAsync(u => u.UserName == "sv01");
                context.Students.Add(new Student { UserId = user.Id, StudentCode = "SE001", DepartmentId = se.DepartmentId, IsGraduate = false });
            }

            if (await context.Users.AnyAsync(u => u.UserName == "sv02"))
            {
                var user = await context.Users.FirstAsync(u => u.UserName == "sv02");
                context.Students.Add(new Student { UserId = user.Id, StudentCode = "SE002", DepartmentId = se.DepartmentId, IsGraduate = false });
            }

            await context.SaveChangesAsync();


            // ==========================================
            // PHẦN 4: DỮ LIỆU NGHIỆP VỤ (SECTION, ANNOUNCEMENT)
            // ==========================================

            // Lấy lại thông tin cần thiết
            var lecturerEntity = await context.Lecturers.FirstOrDefaultAsync();
            var courseEntity = await context.Courses.FirstOrDefaultAsync(c => c.CourseCode == "PRN211");
            var adminUserEntity = await context.Users.FirstAsync(u => u.UserName == "admin");

            if (lecturerEntity != null && courseEntity != null)
            {
                var sections = new CourseSection[]
                {
                    new CourseSection
                    {
                        CourseId = courseEntity.CourseId,
                        LecturerId = lecturerEntity.LecturerId,
                        Room = "P301",
                        Capacity = 30,
                        Days = ClassDays.Monday | ClassDays.Wednesday,
                        Sessions = StudySessions.Ca1,
                        DayStart = DateTime.Now,
                        DayEnd = DateTime.Now.AddMonths(3)
                    }
                };
                context.CoursesSections.AddRange(sections);
            }

            var announcements = new Announcement[]
            {
                new Announcement
                {
                    Title = "Thông báo nghỉ tết",
                    Summary = "Lịch nghỉ tết Nguyên Đán",
                    Content = "Toàn trường nghỉ tết từ ngày 20/12 AL đến hết mùng 10 AL.",
                    CreatedDate = DateTime.Now,
                    Taker = RecipientType.All,
                    UserId = adminUserEntity.Id // Dùng Id của Admin
                }
            };
            context.Announcements.AddRange(announcements);

            await context.SaveChangesAsync();
        }
    }
}