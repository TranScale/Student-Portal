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
            // 1. Tạo database nếu chưa có
            await context.Database.EnsureCreatedAsync();

            // 2. Kiểm tra nếu đã có dữ liệu Khoa thì không chạy lại (tránh trùng lặp)
            if (context.Faculties.Any()) return;

            // ==========================================================
            // PHẦN 1: DỮ LIỆU DANH MỤC (KHOA, NGÀNH, MÔN)
            // ==========================================================

            // 1.1 Khoa (Faculties)
            var faculties = new Faculty[]
            {
                new Faculty { FacultyName = "Công nghệ thông tin", FacultyCode = "IT", FacultyDescription = "Đào tạo phần mềm và hệ thống." },
                new Faculty { FacultyName = "Kinh tế", FacultyCode = "ECO", FacultyDescription = "Kinh doanh và quản lý." },
                new Faculty { FacultyName = "Ngôn ngữ", FacultyCode = "LANG", FacultyDescription = "Ngoại ngữ quốc tế." },
                new Faculty { FacultyName = "Điện - Điện tử", FacultyCode = "EE", FacultyDescription = "Kỹ thuật điện." }
            };
            context.Faculties.AddRange(faculties);
            await context.SaveChangesAsync();

            var listFaculties = await context.Faculties.ToListAsync();

            // 1.2 Ngành (Departments)
            var departments = new List<Department>
            {
                new Department { DepartmentName = "Kỹ thuật phần mềm", DepartmentCode = "SE", FacultyId = listFaculties.First(f => f.FacultyCode == "IT").FacultyId },
                new Department { DepartmentName = "An toàn thông tin", DepartmentCode = "IA", FacultyId = listFaculties.First(f => f.FacultyCode == "IT").FacultyId },
                new Department { DepartmentName = "Quản trị kinh doanh", DepartmentCode = "BA", FacultyId = listFaculties.First(f => f.FacultyCode == "ECO").FacultyId },
                new Department { DepartmentName = "Ngôn ngữ Anh", DepartmentCode = "EL", FacultyId = listFaculties.First(f => f.FacultyCode == "LANG").FacultyId },
                new Department { DepartmentName = "Tự động hóa", DepartmentCode = "AU", FacultyId = listFaculties.First(f => f.FacultyCode == "EE").FacultyId }
            };
            context.Departments.AddRange(departments);
            await context.SaveChangesAsync();

            var listDepartments = await context.Departments.ToListAsync();

            // 1.3 Môn học (Courses)
            var courses = new List<Course>
            {
                new Course { CourseName = "Lập trình C# .NET", CourseCode = "PRN211", CourseCredit = 3, DepartmentId = listDepartments.First(d => d.DepartmentCode == "SE").DepartmentId },
                new Course { CourseName = "Cấu trúc dữ liệu", CourseCode = "CSD201", CourseCredit = 3, DepartmentId = listDepartments.First(d => d.DepartmentCode == "SE").DepartmentId },
                new Course { CourseName = "Web Java (JSP/Servlet)", CourseCode = "PRJ301", CourseCredit = 3, DepartmentId = listDepartments.First(d => d.DepartmentCode == "SE").DepartmentId },
                new Course { CourseName = "Kinh tế vi mô", CourseCode = "ECO111", CourseCredit = 3, DepartmentId = listDepartments.First(d => d.DepartmentCode == "BA").DepartmentId },
                new Course { CourseName = "Tiếng Anh học thuật", CourseCode = "ENG101", CourseCredit = 2, DepartmentId = listDepartments.First(d => d.DepartmentCode == "EL").DepartmentId },
                new Course { CourseName = "Mạch điện tử", CourseCode = "EEC101", CourseCredit = 3, DepartmentId = listDepartments.First(d => d.DepartmentCode == "AU").DepartmentId }
            };
            context.Courses.AddRange(courses);
            await context.SaveChangesAsync();

            var listCourses = await context.Courses.ToListAsync();

            // ==========================================================
            // PHẦN 2: USER & ROLE & PROFILE
            // ==========================================================

            // Tạo Role cho Identity
            string[] roleNames = { "Admin", "Lecturer", "Student" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
            }

            string passwordChung = "Student@123";

            // Helper tạo user
            async Task CreateUser(string u, string name, UserRoles roleEnum, string roleString)
            {
                var user = new User
                {
                    UserName = u,
                    Email = $"{u}@university.edu.vn",
                    FullName = name,
                    UserRole = roleEnum, // Gán Enum cho User
                    DateOfBirth = DateTime.Parse("2000-01-01"),
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, passwordChung);
                if (result.Succeeded) await userManager.AddToRoleAsync(user, roleString);
            }

            // 2.0. TẠO GIẢNG VIÊN ẢO (ID = 0) - QUAN TRỌNG NHẤT
            // ------------------------------------------------------------------
            if (!await context.Lecturers.AnyAsync(l => l.LecturerId == 0))
            {
                // Bước 1: Tạo User ảo
                await CreateUser("system", "Chưa phân công", UserRoles.Lecturer, "Lecturer");
                var systemUser = await context.Users.FirstAsync(u => u.UserName == "system");

                // Lấy tạm 1 khoa để gán (bắt buộc phải có khoa)
                var tempFacultyId = listFaculties.First().FacultyId;

                // Bước 2: Dùng SQL Raw để Hack ID = 0 (Bỏ qua cơ chế tự tăng)
                // Lưu ý: [dbo].[Lecturers] là tên bảng trong SQL.
                string sqlInsert0 = "SET IDENTITY_INSERT [dbo].[Lecturers] ON; " +
                                    "INSERT INTO [dbo].[Lecturers] (LecturerId, UserId, FacultyId) VALUES (0, {0}, {1}); " +
                                    "SET IDENTITY_INSERT [dbo].[Lecturers] OFF;";

                await context.Database.ExecuteSqlRawAsync(sqlInsert0, systemUser.Id, tempFacultyId);
            }
            // ------------------------------------------------------------------

            // 2.1 Admin
            await CreateUser("admin", "Quản Trị Hệ Thống", UserRoles.Admin, "Admin");
            var adminUser = await context.Users.FirstAsync(u => u.UserName == "admin");
            context.Admins.Add(new Admin { UserId = adminUser.Id });

            // 2.2 Giảng viên (3 người thật)
            await CreateUser("gv01", "Nguyễn Văn Giảng", UserRoles.Lecturer, "Lecturer");
            await CreateUser("gv02", "Trần Thị Lý", UserRoles.Lecturer, "Lecturer");
            await CreateUser("gv03", "Lê Hùng Cường", UserRoles.Lecturer, "Lecturer");

            var listLecturerUsers = await context.Users.Where(u => u.UserName.StartsWith("gv")).ToListAsync();
            // Gán Profile Lecturer
            if (context.Lecturers.Count() < 4) // < 4 vì đã có 1 ông ID=0 rồi
            {
                context.Lecturers.Add(new Lecturer { UserId = listLecturerUsers.First(u => u.UserName == "gv01").Id, FacultyId = listFaculties.First(f => f.FacultyCode == "IT").FacultyId });
                context.Lecturers.Add(new Lecturer { UserId = listLecturerUsers.First(u => u.UserName == "gv02").Id, FacultyId = listFaculties.First(f => f.FacultyCode == "ECO").FacultyId });
                context.Lecturers.Add(new Lecturer { UserId = listLecturerUsers.First(u => u.UserName == "gv03").Id, FacultyId = listFaculties.First(f => f.FacultyCode == "IT").FacultyId });
            }

            // 2.3 Sinh viên (5 người)
            await CreateUser("sv01", "Nguyễn Văn An", UserRoles.Student, "Student");
            await CreateUser("sv02", "Trần Thị Bích", UserRoles.Student, "Student");
            await CreateUser("sv03", "Lê Văn Cường", UserRoles.Student, "Student");
            await CreateUser("sv04", "Phạm Thị Dung", UserRoles.Student, "Student");
            await CreateUser("sv05", "Đỗ Văn Em", UserRoles.Student, "Student");

            var listStudentUsers = await context.Users.Where(u => u.UserName.StartsWith("sv")).ToListAsync();

            if (!context.Students.Any())
            {
                var seId = listDepartments.First(d => d.DepartmentCode == "SE").DepartmentId;
                var baId = listDepartments.First(d => d.DepartmentCode == "BA").DepartmentId;

                var startDate = new DateTime(2023, 9, 1);

                context.Students.Add(new Student { UserId = listStudentUsers.First(u => u.UserName == "sv01").Id, StudentCode = "SE1701", DepartmentId = seId, StartStudyDate = startDate, IsGraduate = false });
                context.Students.Add(new Student { UserId = listStudentUsers.First(u => u.UserName == "sv02").Id, StudentCode = "BA1702", DepartmentId = baId, StartStudyDate = startDate, IsGraduate = false });
                context.Students.Add(new Student { UserId = listStudentUsers.First(u => u.UserName == "sv03").Id, StudentCode = "SE1703", DepartmentId = seId, StartStudyDate = startDate, IsGraduate = false });
                context.Students.Add(new Student { UserId = listStudentUsers.First(u => u.UserName == "sv04").Id, StudentCode = "SE1704", DepartmentId = seId, StartStudyDate = startDate, IsGraduate = false });
                context.Students.Add(new Student { UserId = listStudentUsers.First(u => u.UserName == "sv05").Id, StudentCode = "BA1705", DepartmentId = baId, StartStudyDate = startDate, IsGraduate = false });
            }

            await context.SaveChangesAsync();

            // ==========================================================
            // PHẦN 3: NGHIỆP VỤ (SEMESTER, LỚP, ENROLLMENT)
            // ==========================================================

            // 3.1 Semester
            DateTime today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = today.AddDays(-1 * diff).Date;

            var semester = new Semester
            {
                SemesterName = "Học kỳ 1 Năm",
                AcademicYear = "2025-2026",
                StartDate = startOfWeek,
                EndDate = startOfWeek.AddMonths(4),
                IsActive = true
            };
            context.Semesters.Add(semester);
            var semester2 = new Semester
            {
                SemesterName = "Học kỳ 2 Năm",
                AcademicYear = "2025-2026",
                StartDate = startOfWeek.AddMonths(5),
                EndDate = startOfWeek.AddMonths(9),
                IsActive = true
            };
            context.Semesters.Add(semester2);
            await context.SaveChangesAsync();

            var activeSemester = await context.Semesters.FirstAsync();

            // 3.2 Lớp học phần (CourseSection)
            var prn211 = listCourses.First(c => c.CourseCode == "PRN211");
            var csd201 = listCourses.First(c => c.CourseCode == "CSD201");
            var eco111 = listCourses.First(c => c.CourseCode == "ECO111");

            var gvIT = await context.Lecturers.FirstAsync(l => l.User.UserName == "gv01");
            var gvEco = await context.Lecturers.FirstAsync(l => l.User.UserName == "gv02");

            var sections = new List<CourseSection>
            {
                new CourseSection
                {
                    CourseId = prn211.CourseId, LecturerId = gvIT.LecturerId, SemesterId = activeSemester.SemesterId,
                    Room = "P.301", Capacity = 30,
                    Days = ClassDays.Monday | ClassDays.Wednesday,
                    Sessions = StudySessions.Ca1,
                    DayStart = activeSemester.StartDate, DayEnd = activeSemester.EndDate
                },
                new CourseSection
                {
                    CourseId = csd201.CourseId, LecturerId = gvIT.LecturerId, SemesterId = activeSemester.SemesterId,
                    Room = "LAB.02", Capacity = 25,
                    Days = ClassDays.Tuesday | ClassDays.Thursday,
                    Sessions = StudySessions.Ca3,
                    DayStart = activeSemester.StartDate, DayEnd = activeSemester.EndDate
                },
                new CourseSection
                {
                    CourseId = eco111.CourseId, LecturerId = gvEco.LecturerId, SemesterId = activeSemester.SemesterId,
                    Room = "P.405", Capacity = 40,
                    Days = ClassDays.Friday,
                    Sessions = StudySessions.Ca2,
                    DayStart = activeSemester.StartDate, DayEnd = activeSemester.EndDate
                }
            };
            context.CoursesSections.AddRange(sections);
            await context.SaveChangesAsync();

            // 3.3 Enrollment & Score
            var sv1 = await context.Students.FirstAsync(s => s.StudentCode == "SE1701");
            var secPrn = sections.First(s => s.CourseId == prn211.CourseId);
            var secCsd = sections.First(s => s.CourseId == csd201.CourseId);

            var enrollments = new List<Enrollment>
            {
                new Enrollment { StudentId = sv1.StudentId, CourseSectionId = secPrn.CourseSectionId, Status = EnrollmentStatus.Pending },
                new Enrollment { StudentId = sv1.StudentId, CourseSectionId = secCsd.CourseSectionId, Status = EnrollmentStatus.Pending }
            };
            context.Enrollments.AddRange(enrollments);

            var scores = new List<Score>
            {
                new Score { StudentId = sv1.StudentId, CourseSectionId = secPrn.CourseSectionId, LecturerId = secPrn.LecturerId, Value = ScoreValues.F, ProcessScore=0, MiddleScore=0, ExamScore=0, FinalScore=0 },
                new Score { StudentId = sv1.StudentId, CourseSectionId = secCsd.CourseSectionId, LecturerId = secCsd.LecturerId, Value = ScoreValues.F, ProcessScore=0, MiddleScore=0, ExamScore=0, FinalScore=0 }
            };
            context.Scores.AddRange(scores);
            await context.SaveChangesAsync();

            // 3.4 Tạo ScheduleItem (Lịch học)
            var scheduleItems = new List<ScheduleItem>();
            foreach (var sec in sections)
            {
                for (DateTime date = sec.DayStart; date <= sec.DayEnd; date = date.AddDays(1))
                {
                    bool isClassDay = false;
                    switch (date.DayOfWeek)
                    {
                        case DayOfWeek.Monday: if ((sec.Days & ClassDays.Monday) != 0) isClassDay = true; break;
                        case DayOfWeek.Tuesday: if ((sec.Days & ClassDays.Tuesday) != 0) isClassDay = true; break;
                        case DayOfWeek.Wednesday: if ((sec.Days & ClassDays.Wednesday) != 0) isClassDay = true; break;
                        case DayOfWeek.Thursday: if ((sec.Days & ClassDays.Thursday) != 0) isClassDay = true; break;
                        case DayOfWeek.Friday: if ((sec.Days & ClassDays.Friday) != 0) isClassDay = true; break;
                        case DayOfWeek.Saturday: if ((sec.Days & ClassDays.Saturday) != 0) isClassDay = true; break;
                        case DayOfWeek.Sunday: if ((sec.Days & ClassDays.Sunday) != 0) isClassDay = true; break;
                    }

                    if (isClassDay)
                    {
                        int weekNum = (date.Subtract(sec.DayStart).Days / 7) + 1;
                        scheduleItems.Add(new ScheduleItem
                        {
                            CourseSectionId = sec.CourseSectionId,
                            ScheduleDate = date,
                            ScheduleWeek = weekNum
                        });
                    }
                }
            }
            if (scheduleItems.Any())
            {
                context.ScheduleItems.AddRange(scheduleItems);
                await context.SaveChangesAsync();
            }

            // ==========================================================
            // PHẦN 4: THÔNG BÁO (ANNOUNCEMENTS)
            // ==========================================================
            var announcements = new List<Announcement>
            {
                new Announcement
                {
                    Title = "Chào mừng năm học mới 2025-2026",
                    Summary = "Lễ khai giảng",
                    Content = "Kính mời toàn thể sinh viên tham gia lễ khai giảng tại Hội trường A vào lúc 8h00 ngày 05/09.",
                    CreatedDate = DateTime.Now,
                    Taker = RecipientType.All,
                    UserId = adminUser.Id
                },
                new Announcement
                {
                    Title = "Thông báo đăng ký tín chỉ đợt 2",
                    Summary = "Mở bổ sung lớp",
                    Content = "Nhà trường mở thêm lớp PRN211 cho sinh viên khóa K17.",
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Taker = RecipientType.Student,
                    UserId = adminUser.Id
                }
            };
            context.Announcements.AddRange(announcements);
            await context.SaveChangesAsync();
        }
    }
}
