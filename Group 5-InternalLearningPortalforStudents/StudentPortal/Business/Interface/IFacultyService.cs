using StudentPortal.Models;

namespace StudentPortal.Business.Interface
{
    public interface IFacultyService
    {
        Task<IEnumerable<Faculty>> GetAllFaculties();
        Task<Faculty?> GetById (int id);
        Task<bool> CreateFaculty(Faculty faculty);
        Task<bool> UpdateFaculty(Faculty faculty);
        Task<bool> DeleteFaculty(int id);
    }
}
