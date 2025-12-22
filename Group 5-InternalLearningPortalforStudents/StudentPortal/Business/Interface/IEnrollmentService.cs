using System.Collections.Generic;
using System.Threading.Tasks;
using StudentPortal.Models;

namespace StudentPortal.Business.Interface
{
    public interface IEnrollmentService
    {
        Task EnrollStudent(int studentId, int sectionId);
        Task UnEnrollStudent(int studentId, int sectionId);

        Task ApproveEnrollment(int enrollmentId);

        Task<List<Enrollment>> GetEnrollmentsByStudent(int studentId);
        Task<List<Enrollment>> GetEnrollmentsBySection(int sectionId);
    }
}
