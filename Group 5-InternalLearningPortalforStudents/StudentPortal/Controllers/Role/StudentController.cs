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
        public async Task<ActionResult> Index(DateTime? date)
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

            //Thời khóa biểu ở trang index
            DateTime selectedDate = date ?? DateTime.Today;
            ViewData["selectedDate"] = selectedDate;

            var studentSchedule = await _context.ScheduleItems
                .Include(s => s.CourseSection).ThenInclude(cs => cs.Course)
                .Include(s => s.CourseSection).ThenInclude(cs => cs.Enrollments)
                .Where(s => s.CourseSection.Enrollments.Any(e => e.StudentId == student.StudentId) && s.ScheduleDate.Date == selectedDate.Date)
                .OrderBy(s => s.CourseSection.Sessions)
                .ToListAsync();

            ViewData["ListSchedule"] = studentSchedule;

            return View();
        }


        //Đợi code sau
        public async Task<ActionResult> Profile()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return RedirectToAction("Login", "Account");

                var currentStudent = await _context.Students
                    .Include(s => s.Department)
                    .ThenInclude(d => d.Faculty)
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);
                if (currentStudent == null) return View("Error");

                ViewData["Name"] = currentStudent.User.FullName;
                ViewData["Department"] = currentStudent.Department.DepartmentName;
                ViewData["City"] = currentStudent.User.City ?? "Chưa có thông tin ";
                ViewData["Email"] = currentStudent.User.Email;
                ViewData["Code"] = currentStudent.StudentCode;
                ViewData["PhoneNumber"] = currentStudent.User.PhoneNumber ?? "Chưa có thông tin";
                ViewData["Facuty"] = currentStudent.Department.Faculty.FacultyName;

                var listEnrollment = await _context.Enrollments
                    .Include(e => e.CourseSection)
                    .ThenInclude(cs => cs.Course)
                    .Where(e => e.StudentId == currentStudent.StudentId)
                    .ToListAsync();

                int totalCredits = listEnrollment.Sum(e => e.CourseSection?.Course?.CourseCredit ?? 0);
                ViewData["TotalCredits"] = totalCredits;
                ViewData["CourseList"] = listEnrollment;
                return View();
            }
            catch(Exception)
            { return View("Error"); }
            
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
                int selectedValue = semesterId ?? (semester.FirstOrDefault()?.Id ?? 1);
                ViewData["SemesterList"] = new SelectList(semester, "Id", "DisplayText",selectedValue);

                var currentSemester = semester.FirstOrDefault(s => s.Id == selectedValue);
                ViewData["CurrentSemesterName"] = currentSemester?.DisplayText;

                var scoreList = await _context.Scores
                    .Include(s => s.CourseSection)
                    .ThenInclude(cs => cs.Course)
                    .Where(s => s.StudentId == student.StudentId && s.CourseSection.SemesterId == selectedValue)
                    .ToListAsync();

                ViewData["ListScores"] = scoreList;

                return View();
            }
            catch(Exception)
            {
                return View("Error");
            }
        }

        public ActionResult Schedule()
        {
            return View();
        }


    }
}
