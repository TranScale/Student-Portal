using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    // ✅ BẮT BUỘC ĐĂNG NHẬP CHO TẤT CẢ
    [Authorize]
    public class UsersController : Controller
    {
        private readonly StudentPortalContext _context;

        public UsersController(StudentPortalContext context)
        {
            _context = context;
        }
        // ✅ Student + Lecturer + Admin đều vào được
        [Authorize(Roles = "Admin,Student,Lecturer")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // ✅ Student + Lecturer + Admin đều vào được
        [Authorize(Roles = "Admin,Student,Lecturer")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // 🔑 Admin mới được tạo
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // 🔑 Admin mới được tạo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // 🔑 Admin mới được edit
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // 🔑 Admin mới được edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // 🔑 Admin mới được delete
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // load user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return RedirectToAction(nameof(Index));

            // 1) xóa bảng phụ thuộc trước (Admin/Student/Lecturer)
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == id);
            if (admin != null) _context.Admins.Remove(admin);

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == id);
            if (student != null)
            {
                // nếu student có Enrollment/Score... thì cũng phải dọn tiếp
                var enrollments = await _context.Enrollments.Where(e => e.StudentId == student.StudentId).ToListAsync();
                if (enrollments.Count > 0) _context.Enrollments.RemoveRange(enrollments);

                var scores = await _context.Scores.Where(sc => sc.StudentId == student.StudentId).ToListAsync();
                if (scores.Count > 0) _context.Scores.RemoveRange(scores);

                _context.Students.Remove(student);
            }

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == id);
            if (lecturer != null)
            {
                // nếu lecturer dính CourseSection/Score... thì dọn tiếp
                var sections = await _context.CoursesSections.Where(cs => cs.LecturerId == lecturer.LecturerId).ToListAsync();
                if (sections.Count > 0) _context.CoursesSections.RemoveRange(sections);

                var scores = await _context.Scores.Where(sc => sc.LecturerId == lecturer.LecturerId).ToListAsync();
                if (scores.Count > 0) _context.Scores.RemoveRange(scores);

                _context.Lecturers.Remove(lecturer);
            }

            // 2) các bảng khác trỏ về User (Announcement/Certificate)
            var anns = await _context.Announcements.Where(a => a.UserId == id).ToListAsync();
            if (anns.Count > 0) _context.Announcements.RemoveRange(anns);

            var certs = await _context.Certificates.Where(c => c.UserId == id).ToListAsync();
            if (certs.Count > 0) _context.Certificates.RemoveRange(certs);

            // 3) cuối cùng mới xóa User
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    }
}

  