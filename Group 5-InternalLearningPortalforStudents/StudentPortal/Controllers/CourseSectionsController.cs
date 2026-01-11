using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using StudentPortal.Data;
using StudentPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentPortal.Controllers
{
    [Authorize]
    public class CourseSectionsController : Controller
    {
        private readonly StudentPortalContext _context;

        public CourseSectionsController(StudentPortalContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int? semesterId, int? facultyId, int? departmentId, int? pageNumber)
        {

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentSemester"] = semesterId;
            ViewData["CurrentFaculty"] = facultyId;
            ViewData["CurrentDepartment"] = departmentId;


            var sections = _context.CoursesSections
                .Include(c => c.Semester)
                .Include(c => c.Lecturer).ThenInclude(l => l.User)
                .Include(c => c.Course).ThenInclude(co => co.Department).ThenInclude(d => d.Faculty)
                .AsQueryable();


            if (!string.IsNullOrEmpty(searchString))
            {
                sections = sections.Where(s => s.Course.CourseName.Contains(searchString)
                                            || s.Course.CourseCode.Contains(searchString)
                                            || s.Room.Contains(searchString));
            }

            if (semesterId.HasValue)
            {
                sections = sections.Where(s => s.SemesterId == semesterId);
            }

            if (facultyId.HasValue)
            {
                sections = sections.Where(s => s.Course.Department.FacultyId == facultyId);
            }

            if (departmentId.HasValue)
            {
                sections = sections.Where(s => s.Course.DepartmentId == departmentId);
            }

            sections = sections.OrderByDescending(c => c.CourseSectionId);


            ViewBag.Semesters = new SelectList(_context.Semesters, "SemesterId", "SemesterName", semesterId);

            ViewBag.Faculties = new SelectList(_context.Faculties, "FacultyId", "FacultyName", facultyId);

            var departmentsQuery = _context.Departments.AsQueryable();
            if (facultyId.HasValue)
            {
                departmentsQuery = departmentsQuery.Where(d => d.FacultyId == facultyId);
            }
            ViewBag.Departments = new SelectList(departmentsQuery, "DepartmentId", "DepartmentName", departmentId);

            int pageSize = 10;
            return View(await PaginatedList<CourseSection>.CreateAsync(sections.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var courseSection = await _context.CoursesSections
                .Include(c => c.Course)
                .Include(c => c.Semester)
                .Include(c => c.Lecturer).ThenInclude(l => l.User)
                .FirstOrDefaultAsync(m => m.CourseSectionId == id);

            if (courseSection == null) return NotFound();

            var scoreList = await _context.Scores
                .Include(s => s.Student).ThenInclude(stu => stu.User) 
                .Where(s => s.CourseSectionId == id) 
                .OrderBy(s => s.Student.User.FullName) 
                .ToListAsync();

            ViewData["ScoreList"] = scoreList;

            return View(courseSection);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName");
            var lecturers = _context.Lecturers.Include(l => l.User).Select(l => new { l.LecturerId, l.User.FullName });
            ViewData["LecturerId"] = new SelectList(lecturers, "LecturerId", "FullName");
            ViewData["SemesterId"] = new SelectList(_context.Semesters.OrderByDescending(s => s.StartDate), "SemesterId", "SemesterName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("CourseSectionId,Room,Capacity,Days,Sessions,CourseId,LecturerId,SemesterId")] CourseSection courseSection)
        {
            // 1. Clean ModelState
            ModelState.Remove("DayStart");
            ModelState.Remove("DayEnd");
            ModelState.Remove("Course");
            ModelState.Remove("Lecturer");
            ModelState.Remove("Semester");

            // 2. Validation Custom
            if (courseSection.Days == ClassDays.None)
            {
                ModelState.AddModelError("Days", "Vui lòng chọn ít nhất một ngày học.");
            }
            if (courseSection.Sessions == StudySessions.None)
            {
                ModelState.AddModelError("Sessions", "Vui lòng chọn ít nhất một ca học.");
            }
            if (string.IsNullOrEmpty(courseSection.Room))
            {
                ModelState.AddModelError("Room", "Vui lòng nhập phòng học.");
            }

            // 3. Xử lý Semester
            var semester = await _context.Semesters.FindAsync(courseSection.SemesterId);
            if (semester != null)
            {
                courseSection.DayStart = semester.StartDate;
                courseSection.DayEnd = semester.EndDate;
            }
            else
            {
                ModelState.AddModelError("SemesterId", "Học kỳ không hợp lệ");
            }

            if (ModelState.IsValid)
            {
                var existingSections = _context.CoursesSections
                                               .Where(x => x.SemesterId == courseSection.SemesterId)
                                               .AsEnumerable();

                bool roomConflict = existingSections.Any(x =>
                    x.Room == courseSection.Room &&
                    (x.Days & courseSection.Days) != ClassDays.None &&
                    (x.Sessions & courseSection.Sessions) != StudySessions.None
                );

                if (roomConflict)
                {
                    ModelState.AddModelError("Room", $"Phòng {courseSection.Room} đã bị trùng lịch vào khung giờ này.");
                }

                if (courseSection.LecturerId != 0)
                {
                    bool lecturerConflict = existingSections.Any(x =>
                        x.LecturerId == courseSection.LecturerId &&
                        (x.Days & courseSection.Days) != ClassDays.None &&
                        (x.Sessions & courseSection.Sessions) != StudySessions.None
                    );

                    if (lecturerConflict)
                    {
                        ModelState.AddModelError("LecturerId", "Giảng viên này đang bận dạy lớp khác vào khung giờ này.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(courseSection);
                await _context.SaveChangesAsync();
                await GenerateScheduleItems(courseSection);
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CourseId = new SelectList(_context.Courses, "CourseId", "CourseName", courseSection.CourseId);
            ViewBag.SemesterId = new SelectList(_context.Semesters, "SemesterId", "SemesterName", courseSection.SemesterId);

            // 2. Giảng viên (Lấy list sạch)
            var dbLecturers = _context.Lecturers
                                      .Include(l => l.User)
                                      .Where(l => l.User.UserName != "system")
                                      .Select(l => new {
                                          LecturerId = l.LecturerId,
                                          FullName = l.User.FullName
                                      })
                                      .ToList();

            ViewBag.LecturerId = new SelectList(dbLecturers, "LecturerId", "FullName", courseSection.LecturerId);

            return View(courseSection);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var courseSection = await _context.CoursesSections.FindAsync(id);
            if (courseSection == null) return NotFound();

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName", courseSection.CourseId);
            // Đã sửa: Lọc bỏ system
            var lecturers = _context.Lecturers.Include(l => l.User).Where(l => l.User.UserName != "system").Select(l => new { l.LecturerId, l.User.FullName });
            ViewBag.LecturerId = new SelectList(lecturers, "LecturerId", "FullName", courseSection.LecturerId);
            ViewData["SemesterId"] = new SelectList(_context.Semesters.OrderByDescending(s => s.StartDate), "SemesterId", "SemesterName", courseSection.SemesterId);

            return View(courseSection);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("CourseSectionId,Room,Capacity,Days,Sessions,CourseId,LecturerId,SemesterId")] CourseSection courseSection)
        {
            if (id != courseSection.CourseSectionId) return NotFound();

            ModelState.Remove("DayStart");
            ModelState.Remove("DayEnd");
            ModelState.Remove("Course");
            ModelState.Remove("Lecturer");
            ModelState.Remove("Semester");

            if (courseSection.Days == ClassDays.None)
            {
                ModelState.AddModelError("Days", "Vui lòng chọn ít nhất một ngày học.");
            }
            if (courseSection.Sessions == StudySessions.None)
            {
                ModelState.AddModelError("Sessions", "Vui lòng chọn ít nhất một ca học.");
            }
            if (string.IsNullOrEmpty(courseSection.Room))
            {
                ModelState.AddModelError("Room", "Vui lòng nhập phòng học.");
            }

            var semester = await _context.Semesters.FindAsync(courseSection.SemesterId);
            if (semester != null)
            {
                courseSection.DayStart = semester.StartDate;
                courseSection.DayEnd = semester.EndDate;
            }
            else
            {
                ModelState.AddModelError("SemesterId", "Học kỳ không hợp lệ");
            }

            if (ModelState.IsValid)
            {

                var otherSections = _context.CoursesSections
                                            .Where(x => x.SemesterId == courseSection.SemesterId && x.CourseSectionId != id)
                                            .AsEnumerable();

                bool roomConflict = otherSections.Any(x =>
                    x.Room == courseSection.Room &&
                    (x.Days & courseSection.Days) != ClassDays.None &&
                    (x.Sessions & courseSection.Sessions) != StudySessions.None
                );

                if (roomConflict)
                {
                    ModelState.AddModelError("Room", $"Phòng {courseSection.Room} đã bị trùng lịch với lớp khác.");
                }

                if (courseSection.LecturerId != 0)
                {
                    bool lecturerConflict = otherSections.Any(x =>
                        x.LecturerId == courseSection.LecturerId &&
                        (x.Days & courseSection.Days) != ClassDays.None &&
                        (x.Sessions & courseSection.Sessions) != StudySessions.None
                    );

                    if (lecturerConflict)
                    {
                        ModelState.AddModelError("LecturerId", "Giảng viên này đang bận dạy lớp khác vào khung giờ này.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(courseSection);

                    DeleteScheduleItems(courseSection.CourseSectionId);

                    await _context.SaveChangesAsync();

                    await GenerateScheduleItems(courseSection);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseSectionExists(courseSection.CourseSectionId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }


            // 1. Môn học & Học kỳ (Giữ nguyên)
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName", courseSection.CourseId);
            ViewData["SemesterId"] = new SelectList(_context.Semesters, "SemesterId", "SemesterName", courseSection.SemesterId);

            // 2. Giảng viên: Lấy List sạch từ DB
            var dbLecturers = _context.Lecturers
                                      .Include(l => l.User)
                                      .Where(l => l.User.UserName != "system")
                                      .Select(l => new {
                                          LecturerId = l.LecturerId,
                                          FullName = l.User.FullName
                                      })
                                      .ToList();

            ViewBag.LecturerId = new SelectList(dbLecturers, "LecturerId", "FullName", courseSection.LecturerId);

            return View(courseSection);
        }

        private bool CourseSectionExists(int id)
        {
            return _context.CoursesSections.Any(e => e.CourseSectionId == id);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var courseSection = await _context.CoursesSections.FindAsync(id);

            if (courseSection == null)
            {
                return NotFound();
            }
            bool hasStudents = _context.Enrollments.Any(e => e.CourseSectionId == id);
            if (hasStudents)
            {
                TempData["Error"] = "Lớp này đang có sinh viên học, không thể xóa!";
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            DeleteScheduleItems(id);

            _context.CoursesSections.Remove(courseSection);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        private async Task GenerateScheduleItems(CourseSection section)
        {
            var scheduleItems = new List<ScheduleItem>();
            for (DateTime date = section.DayStart; date <= section.DayEnd; date = date.AddDays(1))
            {
                if (IsClassDay(section.Days, date.DayOfWeek))
                {
                    int weekNum = (date.Subtract(section.DayStart).Days / 7) + 1;
                    scheduleItems.Add(new ScheduleItem
                    {
                        CourseSectionId = section.CourseSectionId,
                        ScheduleDate = date,
                        ScheduleWeek = weekNum
                    });
                }
            }
            if (scheduleItems.Any())
            {
                _context.ScheduleItems.AddRange(scheduleItems);
                await _context.SaveChangesAsync();
            }
        }

        private void DeleteScheduleItems(int courseSectionId)
        {
            var scheduleItems = _context.ScheduleItems
                                        .Where(s => s.CourseSectionId == courseSectionId);

            if (scheduleItems.Any())
            {
                _context.ScheduleItems.RemoveRange(scheduleItems);
            }
        }

        private bool IsClassDay(ClassDays days, DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday: return (days & ClassDays.Monday) != 0;
                case DayOfWeek.Tuesday: return (days & ClassDays.Tuesday) != 0;
                case DayOfWeek.Wednesday: return (days & ClassDays.Wednesday) != 0;
                case DayOfWeek.Thursday: return (days & ClassDays.Thursday) != 0;
                case DayOfWeek.Friday: return (days & ClassDays.Friday) != 0;
                case DayOfWeek.Saturday: return (days & ClassDays.Saturday) != 0;
                case DayOfWeek.Sunday: return (days & ClassDays.Sunday) != 0;
                default: return false;
            }
        }
    }
}