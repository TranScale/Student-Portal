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

            // 2. Check dữ liệu cũ (nếu có Faculty rồi thì thôi không init lại)
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
            // PHẦN 4: DỮ LIỆU NGHIỆP VỤ (HỌC KỲ, LỚP, ENROLLMENT, SCHEDULE)
            // ==========================================

            // 1. Tạo Semester (Lấy ngày hiện tại làm mốc để lúc nào chạy cũng có dữ liệu)
            // Chúng ta set ngày bắt đầu là Thứ 2 tuần này để lịch hiển thị đẹp
            DateTime today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = today.AddDays(-1 * diff).Date;

            var semester = new Semester
            {
                SemesterName = "Spring 2024",
                AcademicYear = "2024",
                StartDate = startOfWeek, // Bắt đầu từ thứ 2 tuần này
                EndDate = startOfWeek.AddMonths(4),
                IsActive = true
            };
            context.Semesters.Add(semester);
            await context.SaveChangesAsync();

            // 2. Tạo các Lớp học (CourseSection)
            var lecturerEntity = await context.Lecturers.FirstOrDefaultAsync();
            var prn211 = await context.Courses.FirstOrDefaultAsync(c => c.CourseCode == "PRN211");
            var csd201 = await context.Courses.FirstOrDefaultAsync(c => c.CourseCode == "CSD201");
            var eco101 = await context.Courses.FirstOrDefaultAsync(c => c.CourseCode == "ECO101");
            var semesterEntity = await context.Semesters.FirstAsync();

            var sections = new List<CourseSection>();

            if (lecturerEntity != null && prn211 != null && csd201 != null)
            {
                // Lớp 1: C# (PRN211) - Học Thứ 2, Thứ 4 - Ca 1 (Sáng)
                sections.Add(new CourseSection
                {
                    CourseId = prn211.CourseId,
                    LecturerId = lecturerEntity.LecturerId,
                    SemesterId = semesterEntity.SemesterId,
                    Room = "P301",
                    Capacity = 30,
                    Days = ClassDays.Monday | ClassDays.Wednesday, // Enum Flags
                    Sessions = StudySessions.Ca1, // Ca sáng
                    DayStart = semesterEntity.StartDate,
                    DayEnd = semesterEntity.EndDate
                });

                // Lớp 2: Cấu trúc dữ liệu (CSD201) - Học Thứ 3, Thứ 5 - Ca 3 (Chiều)
                sections.Add(new CourseSection
                {
                    CourseId = csd201.CourseId,
                    LecturerId = lecturerEntity.LecturerId,
                    SemesterId = semesterEntity.SemesterId,
                    Room = "Lab-02",
                    Capacity = 25,
                    Days = ClassDays.Tuesday | ClassDays.Thursday,
                    Sessions = StudySessions.Ca3, // Ca chiều
                    DayStart = semesterEntity.StartDate,
                    DayEnd = semesterEntity.EndDate
                });


                context.CoursesSections.AddRange(sections);
                await context.SaveChangesAsync();
            }

            // 3. Enrollment (QUAN TRỌNG: Sinh viên phải có Enrollment mới hiện lịch)
            var savedSections = await context.CoursesSections.ToListAsync();
            var studentEntity1 = await context.Students.FirstOrDefaultAsync(s => s.StudentCode == "SE001"); // sv01

            if (studentEntity1 != null && savedSections.Any())
            {
                var enrollments = new List<Enrollment>();
                foreach (var sec in savedSections)
                {
                    // Đăng ký sv01 vào tất cả các lớp vừa tạo
                    enrollments.Add(new Enrollment
                    {
                        CourseSectionId = sec.CourseSectionId,
                        StudentId = studentEntity1.StudentId,
                    });
                }
                context.Enrollments.AddRange(enrollments);
                await context.SaveChangesAsync();
            }

            // 4. Sinh ScheduleItem (Thời khóa biểu chi tiết từng ngày)
            // Logic: Duyệt qua từng lớp, duyệt từ ngày bắt đầu đến kết thúc, nếu trúng thứ trong tuần thì tạo lịch
            var scheduleItems = new List<ScheduleItem>();

            foreach (var section in savedSections)
            {
                // Loop từ ngày bắt đầu đến ngày kết thúc của lớp học
                for (DateTime date = section.DayStart; date <= section.DayEnd; date = date.AddDays(1))
                {
                    // Kiểm tra xem ngày này có khớp với lịch học (Monday, Tuesday...) không
                    // Giả sử ClassDays là Enum Flags. Nếu không dùng Flags thì sửa lại logic if đơn giản.
                    bool isClassDay = false;

                    switch (date.DayOfWeek)
                    {
                        case DayOfWeek.Monday:
                            if ((section.Days & ClassDays.Monday) != 0) isClassDay = true;
                            break;
                        case DayOfWeek.Tuesday:
                            if ((section.Days & ClassDays.Tuesday) != 0) isClassDay = true;
                            break;
                        case DayOfWeek.Wednesday:
                            if ((section.Days & ClassDays.Wednesday) != 0) isClassDay = true;
                            break;
                        case DayOfWeek.Thursday:
                            if ((section.Days & ClassDays.Thursday) != 0) isClassDay = true;
                            break;
                        case DayOfWeek.Friday:
                            if ((section.Days & ClassDays.Friday) != 0) isClassDay = true;
                            break;
                        case DayOfWeek.Saturday:
                            if ((section.Days & ClassDays.Saturday) != 0) isClassDay = true;
                            break;
                        case DayOfWeek.Sunday:
                            if ((section.Days & ClassDays.Sunday) != 0) isClassDay = true;
                            break;
                    }

                    if (isClassDay)
                    {
                        // Tính số tuần (đơn giản hóa: Tuần 1, Tuần 2...)
                        int weekNum = (date.Subtract(section.DayStart).Days / 7) + 1;

                        scheduleItems.Add(new ScheduleItem
                        {
                            CourseSectionId = section.CourseSectionId,
                            ScheduleDate = date,
                            ScheduleWeek = weekNum,
                        });
                    }
                }
            }

            if (scheduleItems.Any())
            {
                context.ScheduleItems.AddRange(scheduleItems);
                await context.SaveChangesAsync();
            }

            // ==========================================
            // PHẦN 5: THÔNG BÁO
            // ==========================================
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