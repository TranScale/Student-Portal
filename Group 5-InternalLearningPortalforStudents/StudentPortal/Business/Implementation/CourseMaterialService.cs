using StudentPortal.Business.Interface;
using StudentPortal.Data;
using StudentPortal.Models;
using Microsoft.EntityFrameworkCore;
namespace StudentPortal.Business.Implementation
{
    public class CourseMaterialService : ICourseMaterialService
    {
        private readonly StudentPortalContext _db;
        private readonly IWebHostEnvironment _env;

        public CourseMaterialService(StudentPortalContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<CourseMaterial> UploadMaterial(
            CourseMaterial material,
            Stream fileStream,
            string originalFileName,
            string? contentType = null)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            if (material.CourseSectionId <= 0) throw new ArgumentException("CourseSectionId invalid");
            if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(originalFileName)) throw new ArgumentException("originalFileName required");

            var sectionOk = await _db.CoursesSections.AnyAsync(s => s.CourseSectionId == material.CourseSectionId);
            if (!sectionOk) throw new InvalidOperationException("Course section not found");

            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploads);

            var ext = Path.GetExtension(originalFileName);
            var safeName = $"{Guid.NewGuid():N}{ext}";
            var physicalPath = Path.Combine(uploads, safeName);

            await using (var fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await fileStream.CopyToAsync(fs);
            }

            var fileInfo = new FileInfo(physicalPath);
            var sizeMb = (float)(fileInfo.Length / 1024.0 / 1024.0);

            material.FileUrl = $"/uploads/{safeName}";
            material.FileSize = sizeMb;
            material.CreationDate = DateTime.UtcNow;

            _db.CoursesMaterials.Add(material);
            await _db.SaveChangesAsync();

            return material;
        }

        public async Task UpdateMaterial(CourseMaterial material)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));

            var existing = await _db.CoursesMaterials.FirstOrDefaultAsync(m => m.CourseMaterialId == material.CourseMaterialId);
            if (existing == null) throw new InvalidOperationException("Material not found");

            existing.Title = material.Title;
            existing.IsPublic = material.IsPublic;

            await _db.SaveChangesAsync();
        }

        public async Task DeleteMaterial(int materialId)
        {
            var m = await _db.CoursesMaterials.FirstOrDefaultAsync(x => x.CourseMaterialId == materialId);
            if (m == null) return;

            // xóa file vật lý
            if (!string.IsNullOrWhiteSpace(m.FileUrl) && m.FileUrl.StartsWith("/"))
            {
                var rel = m.FileUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                var physicalPath = Path.Combine(_env.WebRootPath, rel);
                if (File.Exists(physicalPath)) File.Delete(physicalPath);
            }

            _db.CoursesMaterials.Remove(m);
            await _db.SaveChangesAsync();
        }

        public Task<List<CourseMaterial>> GetMaterialsBySection(int sectionId)
            => _db.CoursesMaterials
                  .Where(m => m.CourseSectionId == sectionId)
                  .OrderByDescending(m => m.CreationDate)
                  .ToListAsync();

        public async Task<(byte[] Bytes, string FileName, string ContentType)> DownloadMaterial(int materialId)
        {
            var m = await _db.CoursesMaterials.FirstOrDefaultAsync(x => x.CourseMaterialId == materialId);
            if (m == null) throw new InvalidOperationException("Material not found");

            if (string.IsNullOrWhiteSpace(m.FileUrl))
                throw new InvalidOperationException("FileUrl empty");

            var rel = m.FileUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var physicalPath = Path.Combine(_env.WebRootPath, rel);

            if (!File.Exists(physicalPath))
                throw new FileNotFoundException("File not found on disk", physicalPath);

            var bytes = await File.ReadAllBytesAsync(physicalPath);
            var fileName = Path.GetFileName(physicalPath);

            // content type đơn giản theo extension
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };

            return (bytes, fileName, contentType);
        }
    }


}
