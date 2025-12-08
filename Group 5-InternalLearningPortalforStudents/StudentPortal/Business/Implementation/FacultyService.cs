using StudentPortal.Business.Interface;
using StudentPortal.Data;
using StudentPortal.Data_Access.Repository.Interface;
using StudentPortal.Models;

namespace StudentPortal.Business.Implementation
{
    public class FacultyService : IFacultyService
    {
        private readonly IRepository<Faculty> _repo;

        public FacultyService(IRepository<Faculty> repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Faculty>> GetAllFaculties()
        {
            return await _repo.GetAll();
        }

        public async Task<Faculty?> GetById(int id)
        {
            return await _repo.GetById(id);
        }

        public async Task<bool> CreateFaculty(Faculty faculty)
        {
            try
            {
                await _repo.Add(faculty);
                return true;
            }
            catch 
            { return false; }
        }

        public async Task<bool> UpdateFaculty(Faculty faculty)
        {
            try
            {
                await _repo.Update(faculty);
                return true;
            }
            catch
            { return false; }
        }

        public async Task<bool> DeleteFaculty(int id)
        {
            try
            {
                await _repo.Delete(id);
                return true;
            }
            catch
            { return false; }
        }


    }
}
