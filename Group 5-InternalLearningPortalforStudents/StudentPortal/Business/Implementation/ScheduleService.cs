using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Data_Access.Repository.Interface;
using StudentPortal.Models;
using StudentPortal.Services.Interfaces;

namespace StudentPortal.Services.Implementations
{
    public class ScheduleService : IScheduleService
    {
        private readonly IRepository<ScheduleItem> _scheduleItemRepo;
        private readonly StudentPortalContext _context;

        public ScheduleService(
            IRepository<ScheduleItem> scheduleItemRepo,
            StudentPortalContext context)
        {
            _scheduleItemRepo = scheduleItemRepo;
            _context = context;
        }

        public async Task CreateSchedule(ScheduleItem item)
        {
            await _scheduleItemRepo.Add(item);
        }

        public async Task<List<ScheduleItem>> GetScheduleByStudent(int studentId)
        {
            // Lấy ScheduleItem thông qua Enrollments của Student
            return await _context.ScheduleItems
                .Include(si => si.CourseSection) // Include CourseSection
                .ThenInclude(cs => cs.Enrollments) // ThenInclude Enrollments
                .Where(si => si.CourseSection.Enrollments
                    .Any(e => e.StudentId == studentId))
                .OrderBy(si => si.ScheduleDate) // Sắp xếp theo ngày
                .ToListAsync();
        }

        public async Task<List<ScheduleItem>> GetScheduleByLecturer(int lecturerId)
        {
            // Lấy ScheduleItem của các CourseSection mà Lecturer dạy
            return await _context.ScheduleItems
                .Include(si => si.CourseSection)
                .Where(si => si.CourseSection.LecturerId == lecturerId)
                .OrderBy(si => si.ScheduleDate)
                .ToListAsync();
        }

        public async Task<List<ScheduleItem>> GetScheduleBySection(int sectionId)
        {
            return await _context.ScheduleItems
                .Where(si => si.CourseSectionId == sectionId)
                .OrderBy(si => si.ScheduleWeek)
                .ThenBy(si => si.ScheduleDate)
                .ToListAsync();
        }
    }
}