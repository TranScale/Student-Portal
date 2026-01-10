using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml; // Thư viện Excel
using StudentPortal.Data;
using StudentPortal.Models;

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

        // GET: ImportData
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportStudents(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file Excel.";
                return RedirectToAction("Index");
            }

            // Đảm bảo Role "Student" tồn tại
            if (!await _roleManager.RoleExistsAsync("Student"))
            {
                await _roleManager.CreateAsync(new IdentityRole<int>("Student"));
            }

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

                        // Bắt đầu đọc từ dòng 2 (bỏ qua tiêu đề)
                        for (int row = 2; row <= rowCount; row++)
                        {
                            // 1. Đọc dữ liệu từ Excel
                            var studentCode = worksheet.Cells[row, 1].Value?.ToString()?.Trim(); // Mã SV
                            var fullName = worksheet.Cells[row, 2].Value?.ToString()?.Trim();    // Họ tên
                            var email = worksheet.Cells[row, 3].Value?.ToString()?.Trim();       // Email

                            // Cột D: ID Bộ môn (Số nguyên). Ví dụ: 1, 2, 3...
                            var deptIdString = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                            int departmentId = 1; // Mặc định ID = 1 nếu file excel để trống hoặc sai
                            int.TryParse(deptIdString, out departmentId);

                            if (string.IsNullOrEmpty(studentCode) || string.IsNullOrEmpty(email))
                            {
                                continue;
                            }

                            // 2. Tạo User (Identity)
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

                                // 3. Tạo Student (Model của bạn)
                                var student = new Student
                                {
                                    UserId = user.Id,
                                    StudentCode = studentCode,

                                    // Ngày nhập học (Lấy ngày hiện tại)
                                    StartStudyDate = DateTime.Now,

                                    // DepartmentId (Lấy từ Excel hoặc mặc định)
                                    DepartmentId = departmentId,

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
    }
}