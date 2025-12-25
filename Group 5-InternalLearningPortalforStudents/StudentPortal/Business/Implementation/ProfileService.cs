using Microsoft.EntityFrameworkCore;
using StudentPortal.Business.Interface; 
using StudentPortal.Data;
using StudentPortal.Data_Access.Repository.Interface;
using StudentPortal.Models;

namespace StudentPortal.Business.Implementation
{
    public class ProfileService : IProfileService
    {
        private readonly IRepository<Admin> _adminRepo;
        private readonly IRepository<Lecturer> _lecturerRepo;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<User> _userRepo;
        private readonly StudentPortalContext _context;

        public ProfileService(
            IRepository<Admin> adminRepo,
            IRepository<Lecturer> lecturerRepo,
            IRepository<Student> studentRepo,
            IRepository<User> userRepo,
            StudentPortalContext context)
        {
            _adminRepo = adminRepo;
            _lecturerRepo = lecturerRepo;
            _studentRepo = studentRepo;
            _userRepo = userRepo;
            _context = context;
        }

        public async Task<Admin?> GetAdminProfile(int adminId)
        {
            // Admin có include User
            return await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AdminId == adminId);
        }

        public async Task<Lecturer?> GetLecturerProfile(int lecturerId)
        {
            return await _context.Lecturers
                .Include(l => l.User)
                .Include(l => l.Faculty)
                .FirstOrDefaultAsync(l => l.LecturerId == lecturerId);
        }

        public async Task<Student?> GetStudentProfile(int studentId)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Department)
                    .ThenInclude(d => d.Faculty) // Include cả Faculty của Department
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
        }

        public async Task<bool> UpdateUserProfile(User user)
        {
            try
            {
                await _userRepo.Update(user);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}