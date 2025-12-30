using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Business;
using StudentPortal.Business.Implementation;
using StudentPortal.Data;
using StudentPortal.Models;
using StudentPortal.Services.Implementations;
using System.Threading.Tasks;

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


        //Đợi code sau
        public ActionResult Profile()
        {
            return View();
        }

        public async Task<IActionResult> Score(int? semesterId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if(currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == currentUser.Id);
                if(student == null)
                {
                    return View("Error");
                }

                DateTime studentDateStudy = student.StartStudyDate.ToDateTime(TimeOnly.MinValue);

                var semester = await _context.Semesters
                    .Where(s => s.StartDate.Date >= studentDateStudy)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => new
                    {
                        Id = s.SemesterId,
                        DisplayText = $"{s.SemesterName} - Năm học {s.AcademicYear}"
                    }).ToListAsync();
                int selectedValue = semesterId ?? (semester.FirstOrDefault()?.Id ?? 0);
                ViewData["SemesterList"] = new SelectList(semester, "Id", "DisplayText",selectedValue);
                return View();
            }
            catch(Exception)
            {
                return View("Error");
            }
        }


    }
}
