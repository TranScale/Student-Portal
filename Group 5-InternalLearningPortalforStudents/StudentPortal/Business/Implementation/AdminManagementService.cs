using StudentPortal.Models;
using StudentPortal.Services.Interfaces;
using StudentPortal.Data_Access.Repository.Interface;

namespace StudentPortal.Services.Implementations
{
    public class AdminManagementService : IAdminManagementService
    {
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<Lecturer> _lecturerRepo;
        private readonly IRepository<Admin> _adminRepo;
        private readonly IRepository<User> _userRepo;

        public AdminManagementService(
            IRepository<Student> studentRepo,
            IRepository<Lecturer> lecturerRepo,
            IRepository<Admin> adminRepo,
            IRepository<User> userRepo)
        {
            _studentRepo = studentRepo;
            _lecturerRepo = lecturerRepo;
            _adminRepo = adminRepo;
            _userRepo = userRepo;
        }

        // ========== STUDENT CRUD ==========
        public async Task<Student> CreateStudent(Student student)
        {
            await _studentRepo.Add(student);
            return student;
        }

        public async Task<Student?> GetStudent(int id)
        {
            return await _studentRepo.GetById(id);
        }

        public async Task<bool> UpdateStudent(Student student)
        {
            try
            {
                await _studentRepo.Update(student);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteStudent(int id)
        {
            try
            {
                await _studentRepo.Delete(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Student>> GetAllStudents()
        {
            var students = await _studentRepo.GetAll();
            return students.ToList();
        }

        // ========== LECTURER CRUD ==========
        public async Task<Lecturer> CreateLecturer(Lecturer lecturer)
        {
            await _lecturerRepo.Add(lecturer);
            return lecturer;
        }

        public async Task<Lecturer?> GetLecturer(int id)
        {
            return await _lecturerRepo.GetById(id);
        }

        public async Task<bool> UpdateLecturer(Lecturer lecturer)
        {
            try
            {
                await _lecturerRepo.Update(lecturer);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteLecturer(int id)
        {
            try
            {
                await _lecturerRepo.Delete(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Lecturer>> GetAllLecturers()
        {
            var lecturers = await _lecturerRepo.GetAll();
            return lecturers.ToList();
        }

        // ========== ADMIN CRUD ==========
        public async Task<Admin> CreateAdmin(Admin admin)
        {
            await _adminRepo.Add(admin);
            return admin;
        }

        public async Task<Admin?> GetAdmin(int id)
        {
            return await _adminRepo.GetById(id);
        }

        public async Task<bool> UpdateAdmin(Admin admin)
        {
            try
            {
                await _adminRepo.Update(admin);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAdmin(int id)
        {
            try
            {
                await _adminRepo.Delete(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Admin>> GetAllAdmins()
        {
            var admins = await _adminRepo.GetAll();
            return admins.ToList();
        }
    }
}