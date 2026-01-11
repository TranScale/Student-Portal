using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Cần thêm để dùng ToListAsync
using OfficeOpenXml;
using StudentPortal.Data;
using StudentPortal.Models;
using System.Data;

namespace StudentPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ImportDataController : Controller
    {
        private readonly StudentPortalContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public ImportDataController(StudentPortalContext context, UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Hàm phụ trợ để chuẩn hóa chuỗi (Lowercase + Trim)
        private string NormalizeString(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return input.Trim().ToLower();
        }

        [HttpPost]
        public async Task<IActionResult> ImportStudents(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file Excel.";
                return RedirectToAction("Index");
            }

            if (!await _roleManager.RoleExistsAsync("Student"))
            {
                await _roleManager.CreateAsync(new IdentityRole<int>("Student"));
            }

            // --- BƯỚC 1: Tải danh sách Ngành (Department) lên bộ nhớ và chuẩn hóa ---
            // Key: Tên ngành viết thường, Value: ID Ngành
            var departments = await _context.Departments.ToListAsync();
            var departmentDict = new Dictionary<string, int>();

            foreach (var dept in departments)
            {
                var key = NormalizeString(dept.DepartmentName);
                if (!departmentDict.ContainsKey(key))
                {
                    departmentDict.Add(key, dept.DepartmentId);
                }
            }
            // ------------------------------------------------------------------------

            int successCount = 0;
            int errorCount = 0;
            var errorList = new List<string>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var studentCode = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                            var fullName = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                            var email = worksheet.Cells[row, 3].Value?.ToString()?.Trim();

                            // Đọc TÊN NGÀNH thay vì ID
                            var deptNameInput = worksheet.Cells[row, 4].Value?.ToString();

                            // Kiểm tra dữ liệu cơ bản
                            if (string.IsNullOrEmpty(studentCode) || string.IsNullOrEmpty(email))
                            {
                                continue;
                            }

                            // --- BƯỚC 2: Tìm ID Ngành dựa trên Tên ---
                            int departmentId = 0;
                            var deptKey = NormalizeString(deptNameInput);

                            if (departmentDict.ContainsKey(deptKey))
                            {
                                departmentId = departmentDict[deptKey];
                            }
                            else
                            {
                                // Nếu không tìm thấy tên ngành, báo lỗi dòng này
                                errorCount++;
                                errorList.Add($"Dòng {row}: Không tìm thấy ngành có tên '{deptNameInput}' trong hệ thống.");
                                continue; // Bỏ qua dòng này, không import
                            }
                            // ------------------------------------------

                            var user = new User
                            {
                                UserName = studentCode,
                                Email = email,
                                FullName = fullName,
                                EmailConfirmed = true,
                                PhoneNumber = ""
                            };

                            var result = await _userManager.CreateAsync(user, "Student@123");

                            if (result.Succeeded)
                            {
                                await _userManager.AddToRoleAsync(user, "Student");

                                var student = new Student
                                {
                                    UserId = user.Id,
                                    StudentCode = studentCode,
                                    StartStudyDate = DateTime.Now,
                                    DepartmentId = departmentId, // Sử dụng ID vừa tìm được
                                    IsGraduate = false
                                };

                                _context.Students.Add(student);
                                successCount++;
                            }
                            else
                            {
                                errorCount++;
                                errorList.Add($"Dòng {row}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["Success"] = $"Đã import {successCount} sinh viên. Lỗi {errorCount} dòng.";
                if (errorCount > 0) TempData["ErrorDetail"] = string.Join("\n", errorList);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> ImportLecturers(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file Excel.";
                return RedirectToAction("Index");
            }

            if (!await _roleManager.RoleExistsAsync("Lecturer"))
            {
                await _roleManager.CreateAsync(new IdentityRole<int>("Lecturer"));
            }

            // --- BƯỚC 1: Tải danh sách Khoa (Faculty) lên bộ nhớ và chuẩn hóa ---
            var faculties = await _context.Faculties.ToListAsync();
            var facultyDict = new Dictionary<string, int>();

            foreach (var fac in faculties)
            {
                var key = NormalizeString(fac.FacultyName);
                if (!facultyDict.ContainsKey(key))
                {
                    facultyDict.Add(key, fac.FacultyId);
                }
            }
            // --------------------------------------------------------------------

            int successCount = 0;
            int errorCount = 0;
            var errorList = new List<string>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var lecturerCode = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                            var fullName = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                            var email = worksheet.Cells[row, 3].Value?.ToString()?.Trim();

                            // Đọc TÊN KHOA thay vì ID
                            var facultyNameInput = worksheet.Cells[row, 4].Value?.ToString();

                            if (string.IsNullOrEmpty(lecturerCode) || string.IsNullOrEmpty(email))
                            {
                                continue;
                            }

                            // --- BƯỚC 2: Tìm ID Khoa dựa trên Tên ---
                            int facultyId = 0;
                            var facKey = NormalizeString(facultyNameInput);

                            if (facultyDict.ContainsKey(facKey))
                            {
                                facultyId = facultyDict[facKey];
                            }
                            else
                            {
                                errorCount++;
                                errorList.Add($"Dòng {row}: Không tìm thấy khoa có tên '{facultyNameInput}' trong hệ thống.");
                                continue;
                            }
                            // ----------------------------------------

                            var user = new User
                            {
                                UserName = lecturerCode,
                                Email = email,
                                FullName = fullName,
                                EmailConfirmed = true,
                                PhoneNumber = ""
                            };

                            var result = await _userManager.CreateAsync(user, "Lecturer@123");

                            if (result.Succeeded)
                            {
                                await _userManager.AddToRoleAsync(user, "Lecturer");

                                var lecturer = new Lecturer
                                {
                                    UserId = user.Id,
                                    FacultyId = facultyId, // Sử dụng ID tìm được
                                    IsDeleted = false
                                };

                                _context.Lecturers.Add(lecturer);
                                successCount++;
                            }
                            else
                            {
                                errorCount++;
                                errorList.Add($"Dòng {row}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["Success"] = $"Đã import {successCount} giảng viên. Lỗi {errorCount} dòng.";
                if (errorCount > 0) TempData["ErrorDetail"] = string.Join("\n", errorList);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}