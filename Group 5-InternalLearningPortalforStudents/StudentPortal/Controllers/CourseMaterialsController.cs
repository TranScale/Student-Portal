using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting; // Cần thiết để xử lý file
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;
using System;
using System.Collections.Generic;
using System.IO; // Cần thiết để xử lý đường dẫn
using System.Linq;
using System.Threading.Tasks;

namespace StudentPortal.Controllers
{
    [Authorize]
    public class CourseMaterialsController : Controller
    {
        private readonly StudentPortalContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment; // Inject môi trường để lấy đường dẫn wwwroot

        public CourseMaterialsController(StudentPortalContext context, UserManager<User> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // ================= ĐIỀU HƯỚNG CHUNG =================
        public IActionResult Index()
        {
            if (User.IsInRole("Student")) return RedirectToAction(nameof(StudentIndex));
            if (User.IsInRole("Lecturer")) return RedirectToAction("LecturerIndex"); // Chuyển đến LecturerIndex trong controller này
            return View();
        }

        // ================= KHU VỰC SINH VIÊN =================
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StudentIndex(string searchString, string sortOrder)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var currentStudent = await _context.Students.FirstOrDefaultAsync(s => s.UserId == currentUser.Id);
            if (currentStudent == null) return NotFound();

            var enrollmentsQuery = _context.Enrollments
                .Include(e => e.CourseSection).ThenInclude(cs => cs.Course)
                .Where(e => e.StudentId == currentStudent.StudentId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                enrollmentsQuery = enrollmentsQuery.Where(e =>
                    e.CourseSection.Course.CourseName.Contains(searchString) ||
                    e.CourseSection.Course.CourseCode.Contains(searchString));
            }

            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            switch (sortOrder)
            {
                case "name_desc": enrollmentsQuery = enrollmentsQuery.OrderByDescending(e => e.CourseSection.Course.CourseName); break;
                default: enrollmentsQuery = enrollmentsQuery.OrderBy(e => e.CourseSection.Course.CourseName); break;
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["Courses"] = await enrollmentsQuery.ToListAsync();
            return View();
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StudentDetails(int? id, string searchString)
        {
            if (id == null) return NotFound();

            var courseSection = await _context.CoursesSections
                .Include(cs => cs.Course)
                .FirstOrDefaultAsync(m => m.CourseSectionId == id);

            if (courseSection == null) return NotFound();

            // Sinh viên chỉ thấy file Public
            var materialsQuery = _context.CoursesMaterials
                .Where(m => m.CourseSectionId == id && m.IsPublic == true)
                .OrderByDescending(m => m.CreationDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                materialsQuery = materialsQuery.Where(s => s.Title.Contains(searchString));
            }

            var materials = await materialsQuery.ToListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Trả về PartialView nếu dùng AJAX search
                return PartialView("_MaterialTable", materials);
            }

            ViewData["CourseName"] = $"[{courseSection.Course.CourseCode}] {courseSection.Course.CourseName}";
            ViewData["SectionId"] = id;
            ViewData["CurrentFilter"] = searchString;

            return View(materials);
        }

        // ================= KHU VỰC GIẢNG VIÊN =================

        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> LecturerIndex(string searchString, string sortOrder, int? semesterId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var currentLecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == currentUser.Id);
            if (currentLecturer == null) return NotFound();

            var sectionsQuery = _context.CoursesSections
                .Include(cs => cs.Course)
                .Include(cs => cs.Semester)
                .Where(cs => cs.LecturerId == currentLecturer.LecturerId)
                .AsQueryable();

            var semesters = await _context.Semesters.OrderByDescending(s => s.SemesterId).ToListAsync();
            ViewData["SemesterList"] = new SelectList(semesters, "SemesterId", "SemesterName", semesterId);
            ViewData["CurrentSemester"] = semesterId;

            if (semesterId.HasValue && semesterId.Value > 0)
            {
                sectionsQuery = sectionsQuery.Where(cs => cs.SemesterId == semesterId.Value);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                sectionsQuery = sectionsQuery.Where(cs =>
                    cs.Course.CourseName.Contains(searchString) ||
                    cs.Course.CourseCode.Contains(searchString));
            }

            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["CurrentFilter"] = searchString;

            switch (sortOrder)
            {
                case "name_desc": sectionsQuery = sectionsQuery.OrderByDescending(cs => cs.Course.CourseName); break;
                default: sectionsQuery = sectionsQuery.OrderBy(cs => cs.Course.CourseName); break;
            }

            ViewData["MyClasses"] = await sectionsQuery.ToListAsync();
            return View();
        }

        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> LecturerDetails(int? id, string searchString)
        {
            if (id == null) return NotFound();

            var courseSection = await _context.CoursesSections
                .Include(cs => cs.Course)
                .FirstOrDefaultAsync(m => m.CourseSectionId == id);

            if (courseSection == null) return NotFound();

            // Giảng viên thấy tất cả tài liệu (cả Public và Private)
            var materialsQuery = _context.CoursesMaterials
                .Where(m => m.CourseSectionId == id)
                .OrderByDescending(m => m.CreationDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                materialsQuery = materialsQuery.Where(s => s.Title.Contains(searchString));
            }

            var materials = await materialsQuery.ToListAsync();

            ViewData["CourseName"] = $"[{courseSection.Course.CourseCode}] {courseSection.Course.CourseName}";
            ViewData["SectionId"] = id;
            ViewData["CurrentFilter"] = searchString;

            // Bạn cần tạo View LecturerDetails.cshtml (tương tự StudentDetails nhưng có nút Upload/Delete)
            return View(materials);
        }

        // Action xử lý Upload File (Thay thế cho Create cũ)
        [HttpPost]
        [Authorize(Roles = "Lecturer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadMaterial(int sectionId, IFormFile file, string displayTitle)
        {
            if (file != null && file.Length > 0)
            {
                // 1. Tạo tên file duy nhất để tránh trùng lặp trên server
                var originalFileName = Path.GetFileName(file.FileName);
                var fileExtension = Path.GetExtension(originalFileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}"; // Tên mã hóa

                // 2. Xác định đường dẫn lưu
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "materials");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // 3. Lưu file vật lý
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 4. Lưu vào DB (Map vào Model hiện có của bạn)
                var material = new CourseMaterial
                {
                    Title = string.IsNullOrEmpty(displayTitle) ? originalFileName : displayTitle,
                    FileUrl = uniqueFileName, // Lưu tên file mã hóa vào cột FileUrl

                    // Lưu Bytes. View của bạn đang chia cho 1024*1024 nên ở đây phải lưu Bytes gốc.
                    FileSize = (float)file.Length,

                    CreationDate = DateTime.Now,
                    IsPublic = true,
                    CourseSectionId = sectionId
                };

                _context.Add(material);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(LecturerDetails), new { id = sectionId });
        }

        // Action xử lý Xóa File (Thay thế cho Delete cũ)
        [HttpPost]
        [Authorize(Roles = "Lecturer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMaterial(int id, int sectionId)
        {
            var material = await _context.CoursesMaterials.FindAsync(id);
            if (material != null)
            {
                // 1. Xóa file vật lý
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "materials");
                var filePath = Path.Combine(uploadsFolder, material.FileUrl);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // 2. Xóa dữ liệu DB
                _context.CoursesMaterials.Remove(material);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(LecturerDetails), new { id = sectionId });
        }

        // Action tải file (Dùng chung)
        [Authorize]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var material = await _context.CoursesMaterials.FindAsync(id);
            if (material == null) return NotFound();

            // Đảm bảo đường dẫn này khớp 100% với thư mục bạn lưu file khi Upload
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "materials", material.FileUrl);

            if (!System.IO.File.Exists(filePath))
                return NotFound("File không tồn tại trên hệ thống. Kiểm tra thư mục: " + filePath);

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // Lấy phần mở rộng từ FileUrl (ví dụ: .docx)
            var extension = Path.GetExtension(material.FileUrl);

            // Kiểm tra nếu Title của bạn đã chứa đuôi file chưa để tránh: Hehehe.docx.docx
            var downloadName = material.Title;
            if (!downloadName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                downloadName += extension;
            }

            // Xác định MIME type (nếu không biết rõ dùng application/octet-stream)
            return File(fileBytes, "application/octet-stream", downloadName);
        }

        // Action hỗ trợ AJAX Search cho Giảng viên (nếu cần)
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> GetMaterialsTable(int sectionId, string searchString)
        {
            var query = _context.CoursesMaterials.Where(m => m.CourseSectionId == sectionId);
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(m => m.Title.Contains(searchString));
            }

            var materials = await query.OrderByDescending(m => m.CreationDate).ToListAsync();
            if(User.IsInRole("Student")) return PartialView("_MaterialTable", materials);
            if (User.IsInRole("Lecturer")) return PartialView("_LecturerMaterialTable", materials);

            return View();
        }
    }
}