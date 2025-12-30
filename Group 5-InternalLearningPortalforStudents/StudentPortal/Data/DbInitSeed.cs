using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Models;

namespace StudentPortal.Data
{
    public static class DbInitSeed
    {
        public static async Task InitializeAsync(StudentPortalContext context,
                                                 UserManager<User> userManager,
                                                 RoleManager<IdentityRole<int>> roleManager)
        {
            // 1. Tạo database
            await context.Database.EnsureCreatedAsync();

            // 2. Check dữ liệu cũ
            if (context.Faculties.Any()) return;

            // ==========================================
            // PHẦN 1: DỮ LIỆU DANH MỤC (KHOA, NGÀNH, MÔN)
            // ==========================================
            var faculties = new Faculty[]
            {
                new Faculty { FacultyName = "Công nghệ thông tin", FacultyCode = "CNTT", FacultyDescription = "Khoa đào tạo CNTT" },
                new Faculty { FacultyName = "Kinh tế", FacultyCode = "KT", FacultyDescription = "Khoa Kinh tế và Quản lý" }
            };
            context.Faculties.AddRange(faculties);
            await context.SaveChangesAsync();

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
            // PHẦN 2: USER & ROLE
            // ==========================================
            string[] roleNames = { "Admin", "Lecturer", "Student" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
            }

            async Task CreateUserWithRole(User user, string password, string role)
            {
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded) await userManager.AddToRoleAsync(user, role);
            }

            var passwordChung = "Student@123";

            var adminUser = new User { UserName = "admin", Email = "admin@portal.com", FullName = "Quản trị viên", UserRole = UserRoles.Admin, DateOfBirth = DateTime.Parse("1990-01-01"), EmailConfirmed = true };
            await CreateUserWithRole(adminUser, passwordChung, "Admin");

            var lecturerUser = new User { UserName = "gv01", Email = "giangnv@portal.com", FullName = "Nguyễn Văn Giảng", UserRole = UserRoles.Lecturer, DateOfBirth = DateTime.Parse("1985-05-15"), EmailConfirmed = true };
            await CreateUserWithRole(lecturerUser, passwordChung, "Lecturer");

            var studentUser1 = new User { UserName = "sv01", Email = "troth@portal.com", FullName = "Trần Học Trò", UserRole = UserRoles.Student, DateOfBirth = DateTime.Parse("2003-08-20"), EmailConfirmed = true };
            await CreateUserWithRole(studentUser1, passwordChung, "Student");

            var studentUser2 = new User { UserName = "sv02", Email = "buoilt@portal.com", FullName = "Lê Thị Bưởi", UserRole = UserRoles.Student, DateOfBirth = DateTime.Parse("2003-09-10"), EmailConfirmed = true };
            await CreateUserWithRole(studentUser2, passwordChung, "Student");

            // ==========================================
            // PHẦN 3: PROFILE
            // ==========================================
            var userAdmin = await context.Users.FirstAsync(u => u.UserName == "admin");
            if (!context.Admins.Any()) context.Admins.Add(new Admin { UserId = userAdmin.Id });

            var userLecturer = await context.Users.FirstAsync(u => u.UserName == "gv01");
            if (!context.Lecturers.Any()) context.Lecturers.Add(new Lecturer { UserId = userLecturer.Id, FacultyId = cntt.FacultyId });

            var userSv1 = await context.Users.FirstAsync(u => u.UserName == "sv01");
            if (!context.Students.Any()) context.Students.Add(new Student { UserId = userSv1.Id, StudentCode = "SE001", DepartmentId = se.DepartmentId, IsGraduate = false });

            var userSv2 = await context.Users.FirstAsync(u => u.UserName == "sv02");
            if (!context.Students.Any()) context.Students.Add(new Student { UserId = userSv2.Id, StudentCode = "SE002", DepartmentId = se.DepartmentId, IsGraduate = false });

            await context.SaveChangesAsync();

            // ==========================================
            // PHẦN 4: DỮ LIỆU NGHIỆP VỤ (SEMESTER & SECTION)
            // ==========================================

            // --- 1. THÊM SEMESTER (BẮT BUỘC ĐỂ CÓ ID) ---
            var semester = new Semester
            {
                SemesterName = "Spring 2024",
                AcademicYear = "2024",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(4),
                IsActive = true
            };
            context.Semesters.Add(semester);
            await context.SaveChangesAsync(); // Lưu ngay để lấy SemesterId

            // --- 2. TẠO LỚP HỌC (GẮN VỚI SEMESTER VỪA TẠO) ---
            var lecturerEntity = await context.Lecturers.FirstOrDefaultAsync();
            var courseEntity = await context.Courses.FirstOrDefaultAsync(c => c.CourseCode == "PRN211");

            // Lấy lại Semester vừa lưu (hoặc dùng biến semester ở trên cũng được vì EF tự tracking)
            var semesterEntity = await context.Semesters.FirstAsync();

            if (lecturerEntity != null && courseEntity != null)
            {
                var sections = new CourseSection[]
                {
                    new CourseSection
                    {
                        CourseId = courseEntity.CourseId,
                        LecturerId = lecturerEntity.LecturerId,
                        SemesterId = semesterEntity.SemesterId, 
                        Room = "P301",
                        Capacity = 30,
                        Days = ClassDays.Monday | ClassDays.Wednesday,
                        Sessions = StudySessions.Ca1,
                        DayStart = semesterEntity.StartDate,
                        DayEnd = semesterEntity.EndDate
                    }
                };
                context.CoursesSections.AddRange(sections);
            }

            // Tạo thông báo
            var adminUserEntity = await context.Users.FirstAsync(u => u.UserName == "admin");
            var announcements = new Announcement[]
            {
                new Announcement
                {
                    Title = "Thông báo nghỉ tết",
                    Summary = "Lịch nghỉ tết Nguyên Đán",
                    Content = "Toàn trường nghỉ tết từ ngày 20/12 AL đến hết mùng 10 AL.",
                    CreatedDate = DateTime.Now,
                    Taker = RecipientType.All,
                    UserId = adminUserEntity.Id
                }
            };
            context.Announcements.AddRange(announcements);

            await context.SaveChangesAsync();
        }
    }
}