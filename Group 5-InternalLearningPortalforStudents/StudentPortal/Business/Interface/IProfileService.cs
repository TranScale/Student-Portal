using StudentPortal.Models;

namespace StudentPortal.Business.Interface
{
    public interface IProfileService
    {
        Task<Admin?> GetAdminProfile(int adminId);
        Task<Lecturer?> GetLecturerProfile(int lecturerId);
        Task<Student?> GetStudentProfile(int studentId);
        Task<bool> UpdateUserProfile(User user);
    }
}
