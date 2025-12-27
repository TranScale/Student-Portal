using StudentPortal.Models;
using StudentPortal.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StudentPortal.Data
{
    public static class DbInitSeed
    {
        public static void Initialize(StudentPortalContext context)
        {
            // Kiểm tra database
            context.Database.EnsureCreated();

            // Check xem đã có dữ liệu chưa
            if (context.Faculties.Any())
            {
                return; // Đã có dữ liệu
            }

            // 1. Tạo Khoa (Faculty) - Model này trong file ĐÚNG
            var faculties = new Faculty[]
            {
                new Faculty { FacultyName = "Công nghệ thông tin", FacultyCode = "CNTT", FacultyDescription = "Khoa đào tạo CNTT" },
                new Faculty { FacultyName = "Kinh tế", FacultyCode = "KT", FacultyDescription = "Khoa Kinh tế và Quản lý" }
            };
            context.Faculties.AddRange(faculties);
            context.SaveChanges();

            // 2. Tạo Ngành (Department)
            // LƯU Ý: Model Department trong file bị lỗi hiển thị (hiện là CourseMaterial). 
            // Tôi giả định dùng DepartmentName và DepartmentCode theo chuẩn Faculty.
            var cntt = context.Faculties.First(f => f.FacultyCode == "CNTT");
            var kt = context.Faculties.First(f => f.FacultyCode == "KT");

            var departments = new Department[]
            {
                new Department { DepartmentName = "Kỹ thuật phần mềm", DepartmentCode = "SE", FacultyId = cntt.FacultyId },
                new Department { DepartmentName = "Hệ thống thông tin", DepartmentCode = "IS", FacultyId = cntt.FacultyId },
                new Department { DepartmentName = "Quản trị kinh doanh", DepartmentCode = "BA", FacultyId = kt.FacultyId }
            };
            context.Departments.AddRange(departments);
            context.SaveChanges();

            // 3. Tạo Môn học (Course)
            // LƯU Ý: Model Course trong file bị lỗi hiển thị (hiện là CourseSection).
            // Tôi giả định dùng CourseName, CourseCode, Credits.
            var se = context.Departments.First(d => d.DepartmentCode == "SE");
            var ba = context.Departments.First(d => d.DepartmentCode == "BA");

            var courses = new Course[]
            {
                new Course { CourseName = "Lập trình C# căn bản", CourseCode = "PRN211", CourseCredit = 3, DepartmentId = se.DepartmentId },
                new Course { CourseName = "Cấu trúc dữ liệu", CourseCode = "CSD201", CourseCredit = 3, DepartmentId = se.DepartmentId },
                new Course { CourseName = "Kinh tế vi mô", CourseCode = "ECO101", CourseCredit = 3, DepartmentId = ba.DepartmentId }
            };
            context.Courses.AddRange(courses);
            context.SaveChanges();

            // 4. Tạo Users (Admin, Lecturer, Student)
            // Model User đã kiểm tra kỹ: UserName, Password, FullName...
            var users = new User[]
            {
                // Admin
                new User {
                    UserName = "admin",
                    Password = "123",
                    PasswordHash = "HASH_ADMIN", // Demo hash
                    FullName = "Quản trị viên",
                    Email = "admin@portal.com",
                    UserRole = UserRoles.Admin,
                    DateOfBirth = DateTime.Parse("1990-01-01"),
                    PhoneNumber = "0909000111",
                    Address = "HCM",
                    City = "HCM",
                    Country = "Vietnam"
                },
                // Lecturer
                new User {
                    UserName = "gv01",
                    Password = "123",
                    PasswordHash = "HASH_GV",
                    FullName = "Nguyễn Văn Giảng",
                    Email = "giangnv@portal.com",
                    UserRole = UserRoles.Lecturer,
                    DateOfBirth = DateTime.Parse("1985-05-15")
                },
                // Student 1
                new User {
                    UserName = "sv01",
                    Password = "123",
                    PasswordHash = "HASH_SV",
                    FullName = "Trần Học Trò",
                    Email = "troth@portal.com",
                    UserRole = UserRoles.Student,
                    DateOfBirth = DateTime.Parse("2003-08-20")
                },
                // Student 2
                new User {
                    UserName = "sv02",
                    Password = "123",
                    PasswordHash = "HASH_SV",
                    FullName = "Lê Thị Bưởi",
                    Email = "buoilt@portal.com",
                    UserRole = UserRoles.Student,
                    DateOfBirth = DateTime.Parse("2003-09-10")
                }
            };
            context.Users.AddRange(users);
            context.SaveChanges();

            // 5. Link User vào các bảng chi tiết (Admin, Lecturer, Student)
            var adminUser = context.Users.First(u => u.UserName == "admin");
            var lecturerUser = context.Users.First(u => u.UserName == "gv01");
            var studentUser1 = context.Users.First(u => u.UserName == "sv01");
            var studentUser2 = context.Users.First(u => u.UserName == "sv02");

            // Tạo Admin chi tiết - Model Admin: UserId
            context.Admins.Add(new Admin { UserId = adminUser.UserId });

            // Tạo Lecturer chi tiết - Model Lecturer: UserId, FacultyId
            context.Lecturers.Add(new Lecturer { UserId = lecturerUser.UserId, FacultyId = cntt.FacultyId });

            // Tạo Student chi tiết - Model Student: UserId, StudentCode, DepartmentId, IsGraduate
            context.Students.AddRange(new Student[] {
                new Student { UserId = studentUser1.UserId, StudentCode = "SE001", DepartmentId = se.DepartmentId, IsGraduate = false },
                new Student { UserId = studentUser2.UserId, StudentCode = "SE002", DepartmentId = se.DepartmentId, IsGraduate = false }
            });
            context.SaveChanges();

            // 6. Tạo Lớp học phần (CourseSection)
            // Model CourseSection đã kiểm tra kỹ: Room, Capacity, Days, Sessions, DayStart, DayEnd...
            var courseCsharp = context.Courses.First(c => c.CourseCode == "PRN211");
            var lecturer = context.Lecturers.First(l => l.UserId == lecturerUser.UserId);

            var sections = new CourseSection[]
            {
                new CourseSection
                {
                    CourseId = courseCsharp.CourseId,
                    LecturerId = lecturer.LecturerId,
                    Room = "P301",
                    Capacity = 30,
                    Days = ClassDays.Monday | ClassDays.Wednesday, // Enum Flag
                    Sessions = StudySessions.Ca1, // Enum
                    DayStart = DateTime.Now,
                    DayEnd = DateTime.Now.AddMonths(3)
                }
            };
            context.CoursesSections.AddRange(sections);
            context.SaveChanges();

            // 7. Tạo Thông báo (Announcement)
            // Model Announcement đã kiểm tra kỹ: Title, Summary, Content, CreatedDate, Taker...
            var announcements = new Announcement[]
            {
                new Announcement
                {
                    Title = "Thông báo nghỉ tết",
                    Summary = "Lịch nghỉ tết Nguyên Đán",
                    Content = "Toàn trường nghỉ tết từ ngày 20/12 AL đến hết mùng 10 AL.",
                    CreatedDate = DateTime.Now,
                    Taker = RecipientType.All, // Enum
                    UserId = adminUser.UserId
                }
            };
            context.Announcements.AddRange(announcements);
            context.SaveChanges();
        }
    }
}