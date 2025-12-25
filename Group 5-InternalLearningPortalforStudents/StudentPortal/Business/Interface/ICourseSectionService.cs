using StudentPortal.Models;

namespace StudentPortal.Business.Interface
{
    public interface ICourseSectionService
    {
        Task<CourseSection> CreateCourseSection(CourseSection section);
        Task<List<CourseSection>> GetSectionsByCourse(int courseId);
        Task<List<CourseSection>> GetSectionsByLecturer(int lecturerId);
    }


}
