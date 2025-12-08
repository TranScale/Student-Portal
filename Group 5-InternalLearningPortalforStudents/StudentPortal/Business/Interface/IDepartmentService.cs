using StudentPortal.Models;

namespace StudentPortal.Business.Interface
{
    public interface IDepartmentService
    {
        Task<IEnumerable<Department>> GetDepartmentsByFaculty(int facultyId);
        Task<bool> CreateDepartment(Department department);
        Task<bool> UpdateDepartment(Department department);
        Task<bool> DeleteDepartment(int id);
        Task<Department?> GetDepartmentById(int id);
        Task<IEnumerable<Department>> GetAllDepartments();
    }
}
