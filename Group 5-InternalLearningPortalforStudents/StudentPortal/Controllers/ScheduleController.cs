using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {
        private readonly StudentPortalContext _context;
        private readonly UserManager<User> _userManager;

        public ScheduleController(StudentPortalContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Xem thời khóa biểu (Chung cho GV và SV)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // --- SINH VIÊN ---
            if (User.IsInRole("Student"))
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student != null)
                {
                    // Lấy tất cả các buổi học (ScheduleItem) của các lớp đã đăng ký
                    var scheduleItems = await _context.ScheduleItems
                        .Include(si => si.CourseSection).ThenInclude(cs => cs.Course)
                        .Include(si => si.CourseSection).ThenInclude(cs => cs.Lecturer).ThenInclude(l => l.User)
                        .Where(si => si.CourseSection.Enrollments.Any(e => e.StudentId == student.StudentId && e.Status != EnrollmentStatus.Cancelled))
                        .OrderBy(si => si.ScheduleDate)
                        .ToListAsync();

                    ViewBag.Role = "Student";
                    return View(scheduleItems); // View nhận List<ScheduleItem>
                }
            }
            // --- GIẢNG VIÊN ---
            else if (User.IsInRole("Lecturer"))
            {
                var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
                if (lecturer != null)
                {
                    // Lấy lịch dạy của giảng viên
                    var scheduleItems = await _context.ScheduleItems
                        .Include(si => si.CourseSection).ThenInclude(cs => cs.Course)
                        .Where(si => si.CourseSection.LecturerId == lecturer.LecturerId)
                        .OrderBy(si => si.ScheduleDate)
                        .ToListAsync();

                    ViewBag.Role = "Lecturer";
                    return View(scheduleItems);
                }
            }

            return View(new List<ScheduleItem>());
        }

        // 2. CHECK ĐIỂM DANH (GET) - Chỉ Giảng viên
        [Authorize(Roles = "Lecturer")]
        [HttpGet]
        public async Task<IActionResult> CheckAttendance(int scheduleItemId)
        {
            // Lấy thông tin buổi học
            var scheduleItem = await _context.ScheduleItems
                .Include(s => s.CourseSection).ThenInclude(cs => cs.Course)
                .FirstOrDefaultAsync(s => s.ScheduleItemId == scheduleItemId);

            if (scheduleItem == null) return NotFound();

            // Lấy danh sách sinh viên trong lớp
            var enrollments = await _context.Enrollments
                .Include(e => e.Student).ThenInclude(s => s.User)
                .Where(e => e.CourseSectionId == scheduleItem.CourseSectionId && e.Status == EnrollmentStatus.Approved) // Chỉ lấy sv đã duyệt
                .ToListAsync();

            // Lấy dữ liệu điểm danh đã có (nếu đã điểm danh trước đó)
            var existingAttendees = await _context.Attendees
                .Where(a => a.ScheduleItemId == scheduleItemId)
                .ToListAsync();

            // Tạo ViewModel để hiển thị
            var modelList = new List<Attendee>();

            foreach (var enroll in enrollments)
            {
                var att = existingAttendees.FirstOrDefault(a => a.StudentId == enroll.StudentId);
                if (att == null)
                {
                    // Nếu chưa điểm danh -> Tạo mới object mặc định là Present
                    att = new Attendee
                    {
                        ScheduleItemId = scheduleItemId,
                        StudentId = enroll.StudentId,
                        Student = enroll.Student, // Gán để hiển thị tên
                        Status = AttendanceStatus.Present
                    };
                }
                else
                {
                    att.Student = enroll.Student; // Gán lại để hiển thị tên
                }
                modelList.Add(att);
            }

            ViewBag.ScheduleItem = scheduleItem;
            return View(modelList); // View: form submit list Attendee
        }

        // 3. LƯU ĐIỂM DANH (POST)
        [Authorize(Roles = "Lecturer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAttendance(List<Attendee> attendees, int scheduleItemId)
        {
            if (attendees == null || !attendees.Any()) return RedirectToAction(nameof(Index));

            foreach (var item in attendees)
            {
                // Tìm xem đã có record chưa
                var existing = await _context.Attendees
                    .FirstOrDefaultAsync(a => a.ScheduleItemId == scheduleItemId && a.StudentId == item.StudentId);

                if (existing == null)
                {
                    // Thêm mới
                    var newAtt = new Attendee
                    {
                        ScheduleItemId = scheduleItemId,
                        StudentId = item.StudentId,
                        Status = item.Status,
                        Note = item.Note,
                        RecordedAt = DateTime.Now
                    };
                    _context.Attendees.Add(newAtt);
                }
                else
                {
                    // Cập nhật
                    existing.Status = item.Status;
                    existing.Note = item.Note;
                    existing.RecordedAt = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã lưu điểm danh!";
            return RedirectToAction(nameof(Index)); // Hoặc quay lại trang CheckAttendance
        }
    }
}