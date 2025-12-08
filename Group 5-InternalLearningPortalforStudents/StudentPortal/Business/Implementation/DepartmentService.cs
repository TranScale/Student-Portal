using StudentPortal.Business.Interface;
using StudentPortal.Data_Access.Repository.Interface;
using StudentPortal.Models;

namespace StudentPortal.Business.Implementation
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IRepository<Department> _repo;

        public DepartmentService(IRepository<Department> repo)
        {
            _repo = repo;
        }
        public async Task<bool> CreateDepartment(Department department)
        {
            try
            {
                await _repo.Add(department);
                return true;
            }
            catch {  return false; }
        }

        public async Task<bool> UpdateDepartment(Department department)
        {
            try
            {
                await _repo.Update(department);
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteDepartment(int id)
        {
            try
            {
                await _repo.Delete(id);
                return true;
            }
            catch { return false; }
        }

        public async Task<Department?> GetDepartmentById(int id)
        {
            return await _repo.GetById(id);
        }

        public async Task<IEnumerable<Department>> GetAllDepartments()
        {
            return await _repo.GetAll();
        }

        public async Task<IEnumerable<Department>> GetDepartmentsByFaculty(int facultyId)
        {
            var all = await _repo.GetAll();
            return all.Where(d => d.FacultyId == facultyId);
        }
    }
}
