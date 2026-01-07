using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;

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

        public async Task<IActionResult> Index()
        {
            var sections = _context.CoursesSections
                .Include(c => c.Course)
                .Include(c => c.Lecturer).ThenInclude(l => l.User)
                .Include(c => c.Semester)
                .OrderByDescending(c => c.CourseSectionId);

            return View(await sections.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var courseSection = await _context.CoursesSections
                .Include(c => c.Course)
                .Include(c => c.Lecturer).ThenInclude(l => l.User)
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(m => m.CourseSectionId == id);

            if (courseSection == null) return NotFound();

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
            ModelState.Remove("DayStart");
            ModelState.Remove("DayEnd");
            ModelState.Remove("Course");
            ModelState.Remove("Lecturer");
            ModelState.Remove("Semester");

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
                _context.Add(courseSection);
                await _context.SaveChangesAsync();
                await GenerateScheduleItems(courseSection);
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName", courseSection.CourseId);
            var lecturers = _context.Lecturers.Include(l => l.User).Select(l => new { l.LecturerId, l.User.FullName });
            ViewData["LecturerId"] = new SelectList(lecturers, "LecturerId", "FullName", courseSection.LecturerId);
            ViewData["SemesterId"] = new SelectList(_context.Semesters, "SemesterId", "SemesterName", courseSection.SemesterId);
            return View(courseSection);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var courseSection = await _context.CoursesSections.FindAsync(id);
            if (courseSection == null) return NotFound();

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName", courseSection.CourseId);
            var lecturers = _context.Lecturers.Include(l => l.User).Select(l => new { l.LecturerId, l.User.FullName });
            ViewData["LecturerId"] = new SelectList(lecturers, "LecturerId", "FullName", courseSection.LecturerId);
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

            var semester = await _context.Semesters.FindAsync(courseSection.SemesterId);
            if (semester != null)
            {
                courseSection.DayStart = semester.StartDate;
                courseSection.DayEnd = semester.EndDate;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(courseSection);

                    var oldSchedules = _context.ScheduleItems.Where(s => s.CourseSectionId == id);
                    _context.ScheduleItems.RemoveRange(oldSchedules);

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

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName", courseSection.CourseId);
            var lecturers = _context.Lecturers.Include(l => l.User).Select(l => new { l.LecturerId, l.User.FullName });
            ViewData["LecturerId"] = new SelectList(lecturers, "LecturerId", "FullName", courseSection.LecturerId);
            ViewData["SemesterId"] = new SelectList(_context.Semesters, "SemesterId", "SemesterName", courseSection.SemesterId);
            return View(courseSection);
        }

        private bool CourseSectionExists(int id)
        {
            return _context.CoursesSections.Any(e => e.CourseSectionId == id);
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