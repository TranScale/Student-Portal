using StudentPortal.Models;
using StudentPortal.Data_Access.Repository.Interface;
using StudentPortal.Business.Interface;

namespace StudentPortal.Business.Implementation
{
    public class CourseService : ICourseService
    {
        private readonly IRepository<Course> _repo;
        public CourseService(IRepository<Course> repo)
        {
            _repo = repo;
        }

        public async Task<bool> CreateCourse(Course course)
        {
            try
            {
                await _repo.Add(course);
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateCourse(Course course)
        {
            try
            {
                await _repo.Update(course);
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteCourse(int id)
        {
            try
            {
                await _repo.Delete(id);
                return true;
            }
            catch { return false; }
        }

        public async Task<Course?> GetCourseById(int id)
        {
            return await _repo.GetById(id);
        }

        public async Task<IEnumerable<Course>> GetAllCourses()
        {
            return await _repo.GetAll();
        }

        public async Task<IEnumerable<Course>> GetCoursesByDepartment(int departmentId)
        { 
            var all = await _repo.GetAll();
            return all.Where(c => c.DepartmentId == departmentId);
        }
    }
}
