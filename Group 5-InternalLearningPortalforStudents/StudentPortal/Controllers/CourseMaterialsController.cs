using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Business.Interface; // Namespace chứa ICourseMaterialService
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    [Authorize]
    public class CourseMaterialsController : Controller
    {
        private readonly ICourseMaterialService _materialService;
        private readonly UserManager<User> _userManager;
        private readonly StudentPortalContext _context;

        public CourseMaterialsController(
            ICourseMaterialService materialService,
            UserManager<User> userManager,
            StudentPortalContext context)
        {
            _materialService = materialService;
            _userManager = userManager;
            _context = context;
        }

        // 1. Xem danh sách tài liệu
        public async Task<IActionResult> Index(int sectionId)
        {
            if (!await CanAccessSection(sectionId)) return RedirectToAction("AccessDenied", "Account");

            // Gọi Service lấy list (Service trong file bạn trả về List<CourseMaterial>)
            var materials = await _materialService.GetMaterialsBySection(sectionId);

            ViewBag.SectionId = sectionId;
            return View(materials);
        }

        // 2. Upload - GET
        [Authorize(Roles = "Lecturer")]
        [HttpGet]
        public async Task<IActionResult> Upload(int sectionId)
        {
            if (!await IsLecturerOfSection(sectionId)) return RedirectToAction("AccessDenied", "Account");

            ViewBag.SectionId = sectionId;
            return View();
        }

        // 3. Upload - POST
        [Authorize(Roles = "Lecturer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(CourseMaterial model, IFormFile fileUpload, int sectionId)
        {
            if (!await IsLecturerOfSection(sectionId)) return RedirectToAction("AccessDenied", "Account");

            if (fileUpload != null && fileUpload.Length > 0)
            {
                try
                {
                    model.CourseSectionId = sectionId;

                    // Service của bạn yêu cầu: material, stream, originalName, contentType
                    using (var stream = fileUpload.OpenReadStream())
                    {
                        await _materialService.UploadMaterial(model, stream, fileUpload.FileName, fileUpload.ContentType);
                    }

                    TempData["Success"] = "Upload tài liệu thành công!";
                    return RedirectToAction(nameof(Index), new { sectionId = sectionId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi upload: " + ex.Message);
                }
            }
            else
            {
                ModelState.AddModelError("", "Vui lòng chọn file.");
            }

            ViewBag.SectionId = sectionId;
            return View(model);
        }

        // 4. Download
        public async Task<IActionResult> Download(int id)
        {
            try
            {
                // Vì phương thức DownloadMaterial của bạn trả về byte[], ta cần tự check quyền trước
                // Tìm CourseMaterial trong DB để lấy CourseSectionId check quyền
                // (Lưu ý: _context cần được inject hoặc dùng service khác để get by id)
                var materialInDb = await _context.CoursesMaterials.FindAsync(id);

                if (materialInDb == null) return NotFound();

                if (!await CanAccessSection(materialInDb.CourseSectionId))
                    return RedirectToAction("AccessDenied", "Account");

                // Gọi Service DownloadMaterial (Source 170 trong file của bạn)
                // Hàm này trả về tuple: (byte[] Bytes, string FileName, string ContentType)
                var result = await _materialService.DownloadMaterial(id);

                return File(result.Bytes, result.ContentType, result.FileName);
            }
            catch (InvalidOperationException) // Service ném lỗi này nếu không tìm thấy file
            {
                return NotFound("File không tìm thấy.");
            }
            catch (Exception ex)
            {
                return BadRequest("Lỗi tải file: " + ex.Message);
            }
        }

        // 5. Delete
        [HttpPost]
        [Authorize(Roles = "Lecturer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Tìm material để check quyền trước
            var material = await _context.CoursesMaterials.FindAsync(id);
            if (material == null) return NotFound();

            if (await IsLecturerOfSection(material.CourseSectionId))
            {
                await _materialService.DeleteMaterial(id); // Gọi Service xóa
                TempData["Success"] = "Đã xóa tài liệu.";
                return RedirectToAction(nameof(Index), new { sectionId = material.CourseSectionId });
            }

            return RedirectToAction("AccessDenied", "Account");
        }

        // --- Helpers Check Quyền ---
        private async Task<bool> IsLecturerOfSection(int sectionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return false;
            return await _context.CoursesSections.AnyAsync(c => c.CourseSectionId == sectionId && c.LecturerId == lecturer.LecturerId);
        }

        private async Task<bool> CanAccessSection(int sectionId)
        {
            if (User.IsInRole("Lecturer")) return await IsLecturerOfSection(sectionId);

            if (User.IsInRole("Student"))
            {
                var user = await _userManager.GetUserAsync(User);
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student != null)
                {
                    return await _context.Enrollments.AnyAsync(e =>
                        e.CourseSectionId == sectionId &&
                        e.StudentId == student.StudentId &&
                        e.Status != EnrollmentStatus.Cancelled);
                }
            }
            return false;
        }
    }
}