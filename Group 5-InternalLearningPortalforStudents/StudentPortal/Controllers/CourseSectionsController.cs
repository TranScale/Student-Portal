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

        // ==========================================
        // 1. INDEX & DETAILS
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var sections = _context.CoursesSections
                .Include(c => c.Course)
                .Include(c => c.Lecturer).ThenInclude(l => l.User) // Include User để lấy tên GV
                .Include(c => c.Semester); // Include Semester để hiển thị

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

        // ==========================================
        // 2. CREATE (ADMIN ONLY)
        // ==========================================
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            // Load danh sách Course: Hiển thị Mã + Tên
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName");

            // Load danh sách Lecturer: Hiển thị Tên Giảng Viên (Thay vì ID)
            var lecturers = _context.Lecturers.Include(l => l.User)
                .Select(l => new { LecturerId = l.LecturerId, FullName = l.User.FullName })
                .ToList();
            ViewData["LecturerId"] = new SelectList(lecturers, "LecturerId", "FullName");

            // Load danh sách Semester (Chỉ lấy kỳ đang Active hoặc tương lai)
            ViewData["SemesterId"] = new SelectList(_context.Semesters.OrderByDescending(s => s.StartDate), "SemesterId", "SemesterName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("CourseSectionId,Room,Capacity,Days,Sessions,CourseId,LecturerId,SemesterId")] CourseSection courseSection)
        {
            if (ModelState.IsValid)
            {
                // 1. Ràng buộc ngày: Lấy ngày từ Semester
                var semester = await _context.Semesters.FindAsync(courseSection.SemesterId);
                if (semester != null)
                {
                    courseSection.DayStart = semester.StartDate;
                    courseSection.DayEnd = semester.EndDate;
                }

                // 2. Lưu CourseSection trước để lấy ID
                _context.Add(courseSection);
                await _context.SaveChangesAsync();

                // 3. Tự động sinh ScheduleItems (Thời khóa biểu chi tiết)
                await GenerateScheduleItems(courseSection);

                return RedirectToAction(nameof(Index));
            }

            // Reload data nếu lỗi
            var lecturers = _context.Lecturers.Include(l => l.User).Select(l => new { l.LecturerId, l.User.FullName });
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName", courseSection.CourseId);
            ViewData["LecturerId"] = new SelectList(lecturers, "LecturerId", "FullName", courseSection.LecturerId);
            ViewData["SemesterId"] = new SelectList(_context.Semesters, "SemesterId", "SemesterName", courseSection.SemesterId);
            return View(courseSection);
        }

        // ==========================================
        // 3. EDIT (ADMIN ONLY)
        // ==========================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var courseSection = await _context.CoursesSections.FindAsync(id);
            if (courseSection == null) return NotFound();

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName", courseSection.CourseId);

            var lecturers = _context.Lecturers.Include(l => l.User).Select(l => new { l.LecturerId, l.User.FullName });
            ViewData["LecturerId"] = new SelectList(lecturers, "LecturerId", "FullName", courseSection.LecturerId);

            ViewData["SemesterId"] = new SelectList(_context.Semesters, "SemesterId", "SemesterName", courseSection.SemesterId);

            return View(courseSection);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("CourseSectionId,Room,Capacity,Days,Sessions,CourseId,LecturerId,SemesterId")] CourseSection courseSection)
        {
            if (id != courseSection.CourseSectionId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Cập nhật ngày theo Semester (nếu Semester thay đổi)
                    var semester = await _context.Semesters.FindAsync(courseSection.SemesterId);
                    if (semester != null)
                    {
                        courseSection.DayStart = semester.StartDate;
                        courseSection.DayEnd = semester.EndDate;
                    }

                    _context.Update(courseSection);

                    // 2. Xóa lịch cũ & Tạo lịch mới (Để đồng bộ dữ liệu nếu đổi ngày/thứ)
                    var oldSchedules = _context.ScheduleItems.Where(s => s.CourseSectionId == id);
                    _context.ScheduleItems.RemoveRange(oldSchedules);

                    await _context.SaveChangesAsync(); // Commit xóa và update

                    // 3. Tạo lại lịch mới
                    await GenerateScheduleItems(courseSection);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseSectionExists(courseSection.CourseSectionId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            // Reload view
            var lecturers = _context.Lecturers.Include(l => l.User).Select(l => new { l.LecturerId, l.User.FullName });
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName", courseSection.CourseId);
            ViewData["LecturerId"] = new SelectList(lecturers, "LecturerId", "FullName", courseSection.LecturerId);
            ViewData["SemesterId"] = new SelectList(_context.Semesters, "SemesterId", "SemesterName", courseSection.SemesterId);
            return View(courseSection);
        }

        // ==========================================
        // 4. DELETE & HELPER
        // ==========================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var courseSection = await _context.CoursesSections
                .Include(c => c.Course)
                .Include(c => c.Lecturer).ThenInclude(l => l.User)
                .FirstOrDefaultAsync(m => m.CourseSectionId == id);
            if (courseSection == null) return NotFound();
            return View(courseSection);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var courseSection = await _context.CoursesSections.FindAsync(id);
            if (courseSection != null)
            {
                // ScheduleItems sẽ tự xóa nếu có Cascade Delete, nếu không thì xóa thủ công:
                var schedules = _context.ScheduleItems.Where(s => s.CourseSectionId == id);
                _context.ScheduleItems.RemoveRange(schedules);

                _context.CoursesSections.Remove(courseSection);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CourseSectionExists(int id)
        {
            return _context.CoursesSections.Any(e => e.CourseSectionId == id);
        }

        // ---------------------------------------------------------
        // LOGIC TẠO THỜI KHÓA BIỂU TỰ ĐỘNG
        // ---------------------------------------------------------
        private async Task GenerateScheduleItems(CourseSection section)
        {
            var scheduleItems = new List<ScheduleItem>();

            // Duyệt từ ngày bắt đầu đến kết thúc
            for (DateTime date = section.DayStart; date <= section.DayEnd; date = date.AddDays(1))
            {
                // Kiểm tra xem ngày này (Thứ 2, 3...) có nằm trong lịch học (Days) không
                if (IsClassDay(section.Days, date.DayOfWeek))
                {
                    // Tính tuần học (Tuần 1, Tuần 2...)
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

        // Helper check cờ Enum
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