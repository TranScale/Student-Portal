using StudentPortal.Models;

namespace StudentPortal.Services.Interfaces
{
    public interface IAdminManagementService
    {
        // Student CRUD
        Task<Student> CreateStudent(Student student);
        Task<Student?> GetStudent(int id);
        Task<bool> UpdateStudent(Student student);
        Task<bool> DeleteStudent(int id);
        Task<List<Student>> GetAllStudents();

        // Lecturer CRUD
        Task<Lecturer> CreateLecturer(Lecturer lecturer);
        Task<Lecturer?> GetLecturer(int id);
        Task<bool> UpdateLecturer(Lecturer lecturer);
        Task<bool> DeleteLecturer(int id);
        Task<List<Lecturer>> GetAllLecturers();

        // Admin CRUD
        Task<Admin> CreateAdmin(Admin admin);
        Task<Admin?> GetAdmin(int id);
        Task<bool> UpdateAdmin(Admin admin);
        Task<bool> DeleteAdmin(int id);
        Task<List<Admin>> GetAllAdmins();
    }
}