using StudentPortal.Business.Interface;
using StudentPortal.Models;

namespace StudentPortal.Business.Implementation
{
    public class RoleService : IRoleService
    {
        public RoleService() { }

        public bool Authorize(User user, UserRoles roles)
        {
            //Kiểm tra user có tồn tại không
            if(user == null)
                return false;

            return user.UserRole == roles;
        }

        public bool IsAdmin(User user)
        {
            return user?.UserRole == UserRoles.Admin;
        }

        public bool IsStudent(User user)
        {
            return user?.UserRole == UserRoles.Student;
        }

        public bool IsLecturer(User user)
        {
            return user?.UserRole == UserRoles.Lecturer;
        }

    }
}
