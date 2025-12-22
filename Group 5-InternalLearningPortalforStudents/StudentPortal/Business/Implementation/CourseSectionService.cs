using Microsoft.EntityFrameworkCore;
using StudentPortal.Business.Interface;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Business.Implementation;

public class CourseSectionService : ICourseSectionService
{
    private readonly StudentPortalContext _db;

    public CourseSectionService(StudentPortalContext db) => _db = db;

    public async Task<CourseSection> CreateCourseSection(CourseSection section)
    {
        if (section == null) throw new ArgumentNullException(nameof(section));
        if (section.CourseId <= 0) throw new ArgumentException("CourseId invalid");
        if (section.LecturerId <= 0) throw new ArgumentException("LecturerId invalid");

        // ✅ Course dùng CourseId (không có Id)
        var courseOk = await _db.Courses.AnyAsync(c => c.CourseId == section.CourseId);
        if (!courseOk) throw new InvalidOperationException("Course not found");

        // ✅ Lecturer dùng LecturerId (không có Id)
        var lecturerOk = await _db.Lecturers.AnyAsync(l => l.LecturerId == section.LecturerId);
        if (!lecturerOk) throw new InvalidOperationException("Lecturer not found");

        _db.CoursesSections.Add(section);
        await _db.SaveChangesAsync();
        return section;
    }

    public Task<List<CourseSection>> GetSectionsByCourse(int courseId)
        => _db.CoursesSections
              .Include(s => s.Course)
              .Include(s => s.Lecturer)
              .Where(s => s.CourseId == courseId)
              .ToListAsync();

    public Task<List<CourseSection>> GetSectionsByLecturer(int lecturerId)
        => _db.CoursesSections
              .Include(s => s.Course)
              .Where(s => s.LecturerId == lecturerId)
              .ToListAsync();
}
