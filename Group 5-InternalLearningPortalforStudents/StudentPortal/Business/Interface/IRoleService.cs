using StudentPortal.Models;

namespace StudentPortal.Business.Interface
{
    public interface IRoleService
    {
        public bool Authorize(User user, UserRoles roles);
        public bool IsAdmin(User user);
        public bool IsLecturer(User user);
        public bool IsStudent(User user);
    }
}
