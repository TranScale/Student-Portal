using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentPortal.Models;
using StudentPortal.Services.Implementations;
using System.Threading.Tasks;
using StudentPortal.Business;
using StudentPortal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace StudentPortal.Controllers.Role
{
    [Authorize]
    public class StudentController : Controller
    {
        //Lấy user hiện tại
        private readonly UserManager<User> _userManager;
        private readonly StudentPortalContext _context;
        private readonly SignInManager<User> _signInManager;


        public StudentController(UserManager<User> userManager, StudentPortalContext context, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _context = context;
            _signInManager = signInManager;
        }

        // GET: StudentController
        public async Task<ActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if(currentUser == null)
            {
                await _signInManager.SignOutAsync();
                return Redirect("/Account/Login");
            }

            ViewData["FullName"] = currentUser.FullName;
            ViewData["Email"] = currentUser.Email;
            ViewData["Phone"] = currentUser.PhoneNumber;
            
            var student = await _context.Students.Include(s => s.Department).FirstOrDefaultAsync(s => s.UserId == currentUser.Id);
            if (student == null)
            {
                return View();
            }
            ViewData["StudentCode"] = student.StudentCode;
            ViewData["Deparment"] = student.Department.DepartmentName;

            return View();
        }
    }
}
