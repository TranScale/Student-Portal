using StudentPortal.Models;

namespace StudentPortal.Business.Interface
{
    public interface IScheduleService
    {
        Task CreateSchedule(ScheduleItem item);
        Task<List<ScheduleItem>> GetScheduleByStudent(int studentId);
        Task<List<ScheduleItem>> GetScheduleByLecturer(int lecturerId);
        Task<List<ScheduleItem>> GetScheduleBySection(int sectionId);
    }
}