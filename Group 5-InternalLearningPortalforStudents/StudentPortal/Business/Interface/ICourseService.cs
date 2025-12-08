using StudentPortal.Models;

namespace StudentPortal.Business.Interface
{
    public interface ICourseService
    {
        Task<IEnumerable<Course>> GetCoursesByDepartment(int departmentId);
        Task<bool> CreateCourse(Course course);
        Task<bool> UpdateCourse(Course course);
        Task<bool> DeleteCourse(int id);

        Task<Course?> GetCourseById(int id);
        Task<IEnumerable<Course>> GetAllCourses();
    }
}
