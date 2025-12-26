// StudentPortal.Data/SeedData.cs
using Microsoft.EntityFrameworkCore;
using StudentPortal.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StudentPortal.Data
{
    public static class SeedData
    {
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static void Initialize(StudentPortalContext context)
        {
            // Kiểm tra xem đã có dữ liệu chưa
            if (context.Users.Any())
            {
                Console.WriteLine("Database already seeded.");
                return;
            }

            try
            {
                Console.WriteLine("Seeding database...");

                // 1. Tạo Faculty
                var facultyIT = new Faculty
                {
                    FacultyId = 1,
                    FacultyName = "Khoa Công nghệ Thông tin",
                    FacultyCode = "FIT"
                };
                var facultyMath = new Faculty
                {
                    FacultyId = 2,
                    FacultyName = "Khoa Toán",
                    FacultyCode = "FMATH"
                };
                context.Faculties.AddRange(facultyIT, facultyMath);

                // 2. Tạo Department
                var departmentSE = new Department
                {
                    DepartmentId = 1,
                    DepartmentName = "Kỹ thuật Phần mềm",
                    DepartmentCode = "SE",
                    DepartmentDescription = "Đào tạo kỹ sư phần mềm",
                    FacultyId = 1
                };
                var departmentCS = new Department
                {
                    DepartmentId = 2,
                    DepartmentName = "Khoa học Máy tính",
                    DepartmentCode = "CS",
                    DepartmentDescription = "Đào tạo cử nhân khoa học máy tính",
                    FacultyId = 1
                };
                context.Departments.AddRange(departmentSE, departmentCS);

                // 3. Tạo Users (Students)
                var userStudent1 = new User
                {
                    UserId = 1,
                    UserName = "sv001",
                    FullName = "Nguyễn Văn An",
                    Email = "an.nguyen@example.com",
                    PasswordHash = HashPassword("123456"),
                    UserRole = UserRoles.Student,
                    DateOfBirth = new DateTime(2000, 1, 15),
                    PhoneNumber = "0912345678",
                    Address = "123 Nguyễn Trãi, Ba Đình",
                    City = "Hà Nội",
                    Country = "Việt Nam"
                };

                var userStudent2 = new User
                {
                    UserId = 2,
                    UserName = "sv002",
                    FullName = "Trần Thị Bình",
                    Email = "binh.tran@example.com",
                    PasswordHash = HashPassword("123456"),
                    UserRole = UserRoles.Student,
                    DateOfBirth = new DateTime(2001, 3, 20),
                    PhoneNumber = "0987654321",
                    Address = "456 Lê Lợi, Quận 1",
                    City = "TP. Hồ Chí Minh",
                    Country = "Việt Nam"
                };

                var userStudent3 = new User
                {
                    UserId = 3,
                    UserName = "sv003",
                    FullName = "Lê Văn Cường",
                    Email = "cuong.le@example.com",
                    PasswordHash = HashPassword("123456"),
                    UserRole = UserRoles.Student,
                    DateOfBirth = new DateTime(1999, 8, 10),
                    PhoneNumber = "0909123456",
                    Address = "789 Trần Hưng Đạo, Sơn Trà",
                    City = "Đà Nẵng",
                    Country = "Việt Nam"
                };

                // 4. Tạo Users (Lecturers)
                var userLecturer1 = new User
                {
                    UserId = 4,
                    UserName = "lecturer1",
                    FullName = "TS. Phạm Văn Đức",
                    Email = "duc.pham@example.com",
                    PasswordHash = HashPassword("123456"),
                    UserRole = UserRoles.Lecturer,
                    DateOfBirth = new DateTime(1980, 5, 20),
                    PhoneNumber = "0911222333"
                };

                var userLecturer2 = new User
                {
                    UserId = 5,
                    UserName = "lecturer2",
                    FullName = "PGS.TS. Nguyễn Thị Hồng",
                    Email = "hong.nguyen@example.com",
                    PasswordHash = HashPassword("123456"),
                    UserRole = UserRoles.Lecturer,
                    DateOfBirth = new DateTime(1975, 10, 15),
                    PhoneNumber = "0988333444"
                };

                // 5. Tạo User (Admin)
                var userAdmin = new User
                {
                    UserId = 6,
                    UserName = "admin",
                    FullName = "Quản trị viên hệ thống",
                    Email = "admin@studentportal.edu.vn",
                    PasswordHash = HashPassword("admin123"),
                    UserRole = UserRoles.Admin,
                    DateOfBirth = new DateTime(1990, 1, 1),
                    PhoneNumber = "0900999888"
                };

                context.Users.AddRange(userStudent1, userStudent2, userStudent3,
                                     userLecturer1, userLecturer2, userAdmin);

                // 6. Tạo Students
                var student1 = new Student
                {
                    StudentId = 1,
                    StudentCode = "SV001",
                    IsGraduate = false,
                    UserId = 1,
                    DepartmentId = 1 // SE
                };

                var student2 = new Student
                {
                    StudentId = 2,
                    StudentCode = "SV002",
                    IsGraduate = false,
                    UserId = 2,
                    DepartmentId = 1 // SE
                };

                var student3 = new Student
                {
                    StudentId = 3,
                    StudentCode = "SV003",
                    IsGraduate = false,
                    UserId = 3,
                    DepartmentId = 2 // CS
                };

                context.Students.AddRange(student1, student2, student3);

                // 7. Tạo Admin
                var admin = new Admin
                {
                    AdminId = 1,
                    UserId = 6
                };
                context.Admins.Add(admin);

                // 8. Tạo Lecturers
                var lecturer1 = new Lecturer
                {
                    LecturerId = 1,
                    UserId = 4,
                    FacultyId = 1 // FIT
                };

                var lecturer2 = new Lecturer
                {
                    LecturerId = 2,
                    UserId = 5,
                    FacultyId = 1 // FIT
                };

                context.Lecturers.AddRange(lecturer1, lecturer2);

                // 9. Tạo Courses
                var course1 = new Course
                {
                    CourseId = 1,
                    CourseCode = "SE101",
                    CourseName = "Lập trình C# cơ bản",
                    CourseCredit = 3,
                    DepartmentId = 1 // SE
                };

                var course2 = new Course
                {
                    CourseId = 2,
                    CourseCode = "SE201",
                    CourseName = "Cấu trúc dữ liệu và giải thuật",
                    CourseCredit = 4,
                    DepartmentId = 1 // SE
                };

                var course3 = new Course
                {
                    CourseId = 3,
                    CourseCode = "CS101",
                    CourseName = "Nhập môn lập trình",
                    CourseCredit = 3,
                    DepartmentId = 2 // CS
                };

                var course4 = new Course
                {
                    CourseId = 4,
                    CourseCode = "MATH101",
                    CourseName = "Toán cao cấp",
                    CourseCredit = 3,
                    DepartmentId = 2 // CS
                };

                context.Courses.AddRange(course1, course2, course3, course4);

                // 10. Tạo CourseSections
                var courseSection1 = new CourseSection
                {
                    CourseSectionId = 1,
                    Room = "P.101-A1",
                    Capacity = 50,
                    Days = ClassDays.Monday | ClassDays.Wednesday,
                    DayStart = new DateTime(2024, 1, 1),
                    DayEnd = new DateTime(2024, 5, 31),
                    Sessions = StudySessions.Ca1 | StudySessions.Ca2,
                    CourseId = 1, // SE101
                    LecturerId = 1
                };

                var courseSection2 = new CourseSection
                {
                    CourseSectionId = 2,
                    Room = "P.201-B2",
                    Capacity = 40,
                    Days = ClassDays.Tuesday | ClassDays.Thursday,
                    DayStart = new DateTime(2024, 1, 1),
                    DayEnd = new DateTime(2024, 5, 31),
                    Sessions = StudySessions.Ca3,
                    CourseId = 2, // SE201
                    LecturerId = 1
                };

                var courseSection3 = new CourseSection
                {
                    CourseSectionId = 3,
                    Room = "P.301-C3",
                    Capacity = 60,
                    Days = ClassDays.Monday | ClassDays.Wednesday | ClassDays.Friday,
                    DayStart = new DateTime(2024, 1, 1),
                    DayEnd = new DateTime(2024, 5, 31),
                    Sessions = StudySessions.Ca4,
                    CourseId = 3, // CS101
                    LecturerId = 2
                };

                context.CoursesSections.AddRange(courseSection1, courseSection2, courseSection3);

                // 11. Tạo Enrollments (cho EnrollmentController test)
                var enrollment1 = new Enrollment
                {
                    EnrollmentId = 1,
                    Status = EnrollmentStatus.Approved,
                    CourseSectionId = 1,
                    StudentId = 1
                };

                var enrollment2 = new Enrollment
                {
                    EnrollmentId = 2,
                    Status = EnrollmentStatus.Approved,
                    CourseSectionId = 2,
                    StudentId = 1
                };

                var enrollment3 = new Enrollment
                {
                    EnrollmentId = 3,
                    Status = EnrollmentStatus.Approved,
                    CourseSectionId = 1,
                    StudentId = 2
                };

                var enrollment4 = new Enrollment
                {
                    EnrollmentId = 4,
                    Status = EnrollmentStatus.Pending,
                    CourseSectionId = 3,
                    StudentId = 2
                };

                var enrollment5 = new Enrollment
                {
                    EnrollmentId = 5,
                    Status = EnrollmentStatus.Approved,
                    CourseSectionId = 3,
                    StudentId = 3
                };

                context.Enrollments.AddRange(enrollment1, enrollment2, enrollment3, enrollment4, enrollment5);

                // 12. Tạo Announcements (cho AnnouncementController test)
                var announcement1 = new Announcement
                {
                    AnnouncementId = 1,
                    Title = "Lịch thi giữa kỳ học kỳ Spring 2024",
                    Summary = "Thông báo lịch thi giữa kỳ các môn học",
                    Content = "Kính gửi toàn thể sinh viên,\n\nLịch thi giữa kỳ học kỳ Spring 2024 sẽ diễn ra từ ngày 15/4/2024 đến 20/4/2024. Sinh viên vui lòng kiểm tra lịch thi trên cổng thông tin.",
                    CreatedDate = DateTime.Now.AddDays(-5),
                    ExpiredDate = DateTime.Now.AddDays(30),
                    Taker = RecipientType.Student,
                    UserId = 4 // Lecturer
                };

                var announcement2 = new Announcement
                {
                    AnnouncementId = 2,
                    Title = "Họp giảng viên đầu học kỳ",
                    Summary = "Thông báo cuộc họp giảng viên khoa CNTT",
                    Content = "Kính mời toàn thể giảng viên khoa CNTT tham dự cuộc họp đầu học kỳ vào lúc 14:00 ngày 10/1/2024 tại phòng họp A.",
                    CreatedDate = DateTime.Now.AddDays(-10),
                    ExpiredDate = DateTime.Now.AddDays(10),
                    Taker = RecipientType.Lecturer,
                    UserId = 6 // Admin
                };

                var announcement3 = new Announcement
                {
                    AnnouncementId = 3,
                    Title = "Thông báo nghỉ lễ 30/4 - 1/5",
                    Summary = "Lịch nghỉ lễ Giải phóng Miền Nam và Quốc tế Lao động",
                    Content = "Nhà trường thông báo lịch nghỉ lễ:\n- Ngày 30/4: Nghỉ lễ Giải phóng Miền Nam\n- Ngày 1/5: Nghỉ lễ Quốc tế Lao động\nCác lớp học sẽ được nghỉ và bù vào tuần sau.",
                    CreatedDate = DateTime.Now.AddDays(-2),
                    ExpiredDate = DateTime.Now.AddDays(60),
                    Taker = RecipientType.All,
                    UserId = 6 // Admin
                };

                context.Announcements.AddRange(announcement1, announcement2, announcement3);

                // 13. Tạo Scores (cho ScoreController test)
                var score1 = new Score
                {
                    ScoreId = 1,
                    Value = ScoreValues.A,
                    ProcessScore = 8.5f,
                    MiddleScore = 9.0f,
                    ExamScore = 8.0f,
                    CourseSectionId = 1,
                    StudentId = 1,
                    LecturerId = 1
                };

                var score2 = new Score
                {
                    ScoreId = 2,
                    Value = ScoreValues.B,
                    ProcessScore = 7.0f,
                    MiddleScore = 8.0f,
                    ExamScore = 7.5f,
                    CourseSectionId = 1,
                    StudentId = 2,
                    LecturerId = 1
                };

                var score3 = new Score
                {
                    ScoreId = 3,
                    Value = ScoreValues.C,
                    ProcessScore = 6.5f,
                    MiddleScore = 7.0f,
                    ExamScore = 6.0f,
                    CourseSectionId = 2,
                    StudentId = 1,
                    LecturerId = 1
                };

                var score4 = new Score
                {
                    ScoreId = 4,
                    Value = ScoreValues.A,
                    ProcessScore = 9.0f,
                    MiddleScore = 9.5f,
                    ExamScore = 8.5f,
                    CourseSectionId = 3,
                    StudentId = 3,
                    LecturerId = 2
                };

                context.Scores.AddRange(score1, score2, score3, score4);

                // 14. Tạo ScheduleItems (cho ScheduleController test)
                var baseDate = new DateTime(2024, 1, 1); // Thứ 2 đầu tuần

                var schedule1 = new ScheduleItem
                {
                    ScheduleItemId = 1,
                    ScheduleWeek = 1,
                    ScheduleDate = baseDate.AddDays(0), // Thứ 2
                    CourseSectionId = 1
                };

                var schedule2 = new ScheduleItem
                {
                    ScheduleItemId = 2,
                    ScheduleWeek = 1,
                    ScheduleDate = baseDate.AddDays(2), // Thứ 4
                    CourseSectionId = 1
                };

                var schedule3 = new ScheduleItem
                {
                    ScheduleItemId = 3,
                    ScheduleWeek = 1,
                    ScheduleDate = baseDate.AddDays(1), // Thứ 3
                    CourseSectionId = 2
                };

                var schedule4 = new ScheduleItem
                {
                    ScheduleItemId = 4,
                    ScheduleWeek = 1,
                    ScheduleDate = baseDate.AddDays(3), // Thứ 5
                    CourseSectionId = 2
                };

                var schedule5 = new ScheduleItem
                {
                    ScheduleItemId = 5,
                    ScheduleWeek = 1,
                    ScheduleDate = baseDate.AddDays(4), // Thứ 6
                    CourseSectionId = 3
                };

                // Thêm lịch học tuần 2
                var schedule6 = new ScheduleItem
                {
                    ScheduleItemId = 6,
                    ScheduleWeek = 2,
                    ScheduleDate = baseDate.AddDays(7), // Thứ 2 tuần sau
                    CourseSectionId = 1
                };

                var schedule7 = new ScheduleItem
                {
                    ScheduleItemId = 7,
                    ScheduleWeek = 2,
                    ScheduleDate = baseDate.AddDays(9), // Thứ 4 tuần sau
                    CourseSectionId = 1
                };

                context.ScheduleItems.AddRange(schedule1, schedule2, schedule3, schedule4, schedule5, schedule6, schedule7);

                // Lưu tất cả vào database
                context.SaveChanges();
                Console.WriteLine("Database seeded successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding database: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }
    }
}