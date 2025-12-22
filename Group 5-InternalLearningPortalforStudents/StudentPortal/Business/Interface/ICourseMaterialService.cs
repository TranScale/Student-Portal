using StudentPortal.Models;

namespace StudentPortal.Business.Interface
{
    public interface ICourseMaterialService
    {
        Task<CourseMaterial> UploadMaterial(CourseMaterial material, Stream fileStream, string originalFileName, string? contentType = null);
        Task UpdateMaterial(CourseMaterial material);
        Task DeleteMaterial(int materialId);
        Task<List<CourseMaterial>> GetMaterialsBySection(int sectionId);
        Task<(byte[] Bytes, string FileName, string ContentType)> DownloadMaterial(int materialId);
    }

}


