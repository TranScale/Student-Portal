using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Business.Interface;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Business.Implementation;

public class ScoreService : IScoreService
{
    private readonly StudentPortalContext _db;

    public ScoreService(StudentPortalContext db) => _db = db;

    public async Task<Score> AddScore(Score score)
    {
        if (score == null) throw new ArgumentNullException(nameof(score));
        if (score.StudentId <= 0) throw new ArgumentException("StudentId invalid");
        if (score.CourseSectionId <= 0) throw new ArgumentException("CourseSectionId invalid");
        if (score.LecturerId <= 0) throw new ArgumentException("LecturerId invalid");

        var studentOk = await _db.Students.AnyAsync(s => s.StudentId == score.StudentId);
        if (!studentOk) throw new InvalidOperationException("Student not found");

        var sectionOk = await _db.CoursesSections.AnyAsync(s => s.CourseSectionId == score.CourseSectionId);
        if (!sectionOk) throw new InvalidOperationException("Course section not found");

        var lecturerOk = await _db.Lecturers.AnyAsync(l => l.LecturerId == score.LecturerId);
        if (!lecturerOk) throw new InvalidOperationException("Lecturer not found");

        var existed = await _db.Scores.AnyAsync(x =>
            x.StudentId == score.StudentId && x.CourseSectionId == score.CourseSectionId);

        if (existed) throw new InvalidOperationException("Score already exists. Use UpdateScore.");

        _db.Scores.Add(score);
        await _db.SaveChangesAsync();
        return score;
    }

    public async Task<Score> UpdateScore(Score score)
    {
        if (score == null) throw new ArgumentNullException(nameof(score));

        var existing = await _db.Scores.FirstOrDefaultAsync(x => x.ScoreId == score.ScoreId);
        if (existing == null) throw new InvalidOperationException("Score not found");

        existing.Value = score.Value;
        existing.ProcessScore = score.ProcessScore;
        existing.MiddleScore = score.MiddleScore;
        existing.ExamScore = score.ExamScore;

        await _db.SaveChangesAsync();
        return existing;
    }

    public Task<List<Score>> GetScoresByStudent(int studentId)
        => _db.Scores
              .Include(s => s.CourseSection).ThenInclude(cs => cs.Course)
              .Where(s => s.StudentId == studentId)
              .ToListAsync();

    public Task<List<Score>> GetScoresBySection(int sectionId)
        => _db.Scores
              .Include(s => s.Student)
              .Where(s => s.CourseSectionId == sectionId)
              .ToListAsync();
}
