using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data; 
using StudentPortal.Models;
using StudentPortal.ModelViews;
namespace StudentPortal.Controllers
{
    [Authorize]
    public class AttendeeController : Controller
    {
        private readonly StudentPortalContext _context;

        public AttendeeController(StudentPortalContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> GetAttendanceForm(int scheduleId)
        {
            // 1. Kiểm tra lịch học
            var scheduleItem = await _context.ScheduleItems
                .Include(s => s.CourseSection)
                    .ThenInclude(cs => cs.Course)
                .FirstOrDefaultAsync(s => s.ScheduleItemId == scheduleId);

            if (scheduleItem == null)
            {
                return NotFound(); 
            }

            
            var existingAttendance = await _context.Attendees
                .Include(a => a.Student).ThenInclude(s => s.User)
                .Where(a => a.ScheduleItemId == scheduleId)
                .ToListAsync();

            List<AttendanceViewModel> modelList = new List<AttendanceViewModel>();

            if (existingAttendance.Count > 0)
            {
                
                modelList = existingAttendance.Select(a => new AttendanceViewModel
                {
                    AttendeeId = a.AttendeeId,
                    ScheduleItemId = a.ScheduleItemId,
                    StudentId = a.StudentId,
                    StudentName = a.Student.User.FullName ?? "Chưa cập nhật",
                    StudentCode = a.Student.StudentCode,
                    AvatarPath = a.Student.User.ImagePath,
                    Status = a.Status,
                    Note = a.Note
                }).ToList();
            }
            else
            {
                var enrolledStudents = await _context.Enrollments
                    .Include(e => e.Student).ThenInclude(s => s.User)
                    .Where(e => e.CourseSectionId == scheduleItem.CourseSectionId
                             && e.Status == EnrollmentStatus.Approved && e.Status == EnrollmentStatus.Finished)
                    .Select(e => e.Student)
                    .ToListAsync();

                modelList = enrolledStudents.Select(s => new AttendanceViewModel
                {
                    AttendeeId = 0,
                    ScheduleItemId = scheduleId,
                    StudentId = s.StudentId,
                    StudentName = s.User.FullName ?? "Chưa cập nhật",
                    StudentCode = s.StudentCode,
                    AvatarPath = s.User.ImagePath,
                    Status = AttendanceStatus.Present, 
                    Note = ""
                }).ToList();
            }

            ViewBag.CourseName = scheduleItem.CourseSection.Course.CourseName;
            ViewBag.ScheduleId = scheduleId;

            return PartialView("_AttendanceModal", modelList);
        }

        // 2. POST: Lưu dữ liệu điểm danh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAttendance(List<AttendanceViewModel> attendanceList, int scheduleId)
        {
            if (attendanceList == null || !attendanceList.Any())
            {
                return RedirectToAction("TakeAttendance", new { scheduleId = scheduleId });
            }

            foreach (var item in attendanceList)
            {
                if (item.AttendeeId > 0)
                {
                    // UPDATE: Đã có record, cập nhật trạng thái
                    var existingRecord = await _context.Attendees.FindAsync(item.AttendeeId);
                    if (existingRecord != null)
                    {
                        existingRecord.Status = item.Status;
                        existingRecord.Note = item.Note;
                        existingRecord.RecordedAt = DateTime.Now; // Cập nhật lại giờ sửa
                        _context.Attendees.Update(existingRecord);
                    }
                }
                else
                {
                    // INSERT: Chưa có record, tạo mới
                    var newRecord = new Attendee
                    {
                        ScheduleItemId = item.ScheduleItemId,
                        StudentId = item.StudentId,
                        Status = item.Status,
                        Note = item.Note,
                        RecordedAt = DateTime.Now
                    };
                    _context.Attendees.Add(newRecord);
                }
            }

            await _context.SaveChangesAsync();

            // Lưu xong thì quay về trang Lịch (Schedule) hoặc load lại trang điểm danh và báo thành công
            TempData["SuccessMessage"] = "Đã lưu điểm danh thành công!";
            return RedirectToAction("LecturerSchedule", "Schedule");
        }
    }
}