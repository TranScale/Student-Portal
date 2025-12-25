using Microsoft.EntityFrameworkCore;
using StudentPortal.Business.Interface;
using StudentPortal.Data;
using StudentPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentPortal.Business.Implementation
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly StudentPortalContext _db;

        public EnrollmentService(StudentPortalContext db)
        {
            _db = db;
        }

        public async Task EnrollStudent(int studentId, int sectionId)
        {
            if (studentId <= 0) throw new ArgumentException("studentId invalid");
            if (sectionId <= 0) throw new ArgumentException("sectionId invalid");

            var studentOk = await _db.Students.AnyAsync(s => s.StudentId == studentId);
            if (!studentOk) throw new InvalidOperationException("Student not found");

            var section = await _db.CoursesSections
                .FirstOrDefaultAsync(s => s.CourseSectionId == sectionId);
            if (section == null) throw new InvalidOperationException("Course section not found");

            var existed = await _db.Enrollments.AnyAsync(e =>
                e.StudentId == studentId && e.CourseSectionId == sectionId);
            if (existed) throw new InvalidOperationException("Already enrolled");

            var approvedCount = await _db.Enrollments.CountAsync(e =>
                e.CourseSectionId == sectionId && e.Status == EnrollmentStatus.Approved);

            if (approvedCount >= section.Capacity)
                throw new InvalidOperationException("Section is full");

            var enrollment = new Enrollment
            {
                StudentId = studentId,
                CourseSectionId = sectionId,
                Status = EnrollmentStatus.Pending
            };

            _db.Enrollments.Add(enrollment);
            await _db.SaveChangesAsync();
        }

        public async Task UnEnrollStudent(int studentId, int sectionId)
        {
            if (studentId <= 0) throw new ArgumentException("studentId invalid");
            if (sectionId <= 0) throw new ArgumentException("sectionId invalid");

            var enrollment = await _db.Enrollments.FirstOrDefaultAsync(e =>
                e.StudentId == studentId && e.CourseSectionId == sectionId);

            if (enrollment == null)
                throw new InvalidOperationException("Enrollment not found");

            _db.Enrollments.Remove(enrollment);
            await _db.SaveChangesAsync();
        }

        public async Task ApproveEnrollment(int enrollmentId)
        {
            if (enrollmentId <= 0) throw new ArgumentException("enrollmentId invalid");

            var enrollment = await _db.Enrollments
                .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);

            if (enrollment == null)
                throw new InvalidOperationException("Enrollment not found");

            var section = await _db.CoursesSections
                .FirstOrDefaultAsync(s => s.CourseSectionId == enrollment.CourseSectionId);

            if (section == null)
                throw new InvalidOperationException("Course section not found");

            var approvedCount = await _db.Enrollments.CountAsync(e =>
                e.CourseSectionId == enrollment.CourseSectionId &&
                e.Status == EnrollmentStatus.Approved);

            if (approvedCount >= section.Capacity)
                throw new InvalidOperationException("Section is full");

            enrollment.Status = EnrollmentStatus.Approved;
            await _db.SaveChangesAsync();
        }

        public Task<List<Enrollment>> GetEnrollmentsByStudent(int studentId)
        {
            return _db.Enrollments
                .Include(e => e.CourseSection).ThenInclude(s => s.Course)
                .Include(e => e.CourseSection).ThenInclude(s => s.Lecturer)
                .Where(e => e.StudentId == studentId)
                .ToListAsync();
        }

        public Task<List<Enrollment>> GetEnrollmentsBySection(int sectionId)
        {
            return _db.Enrollments
                .Include(e => e.Student)
                .Where(e => e.CourseSectionId == sectionId)
                .ToListAsync();
        }
    }
}
