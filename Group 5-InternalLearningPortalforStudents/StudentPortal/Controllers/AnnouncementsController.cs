using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    [Authorize]
    public class AnnouncementsController : Controller
    {
        private readonly StudentPortalContext _context;
        private readonly UserManager<User> _userManager;

        public AnnouncementsController(StudentPortalContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Danh sách thông báo (Admin xem toàn bộ)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var announcements = await _context.Announcements
                .Include(a => a.User) // Lấy thông tin người đăng
                .OrderByDescending(a => a.CreatedDate) // Mới nhất lên đầu
                .ToListAsync();
            return View(announcements);
        }

        // 2. Tạo thông báo mới - GET
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // 3. Tạo thông báo mới - POST
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement announcement)
        {
            // Loại bỏ validate cho User và UserId vì ta sẽ tự gán bên dưới
            ModelState.Remove("User");
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser != null)
                {
                    announcement.UserId = currentUser.Id; // Gán ID người đăng
                    announcement.CreatedDate = DateTime.Now; // Gán thời gian hiện tại

                    _context.Add(announcement);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Đã đăng thông báo thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(announcement);
        }

        // 4. Chỉnh sửa - GET
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();

            return View(announcement);
        }

        // 5. Chỉnh sửa - POST
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Announcement announcement)
        {
            if (id != announcement.AnnouncementId) return NotFound();

            // Giữ nguyên người tạo và ngày tạo cũ, không cho sửa
            // Cần lấy dữ liệu cũ từ DB ra để map lại nếu form không gửi lên
            ModelState.Remove("User");
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy bản ghi cũ từ DB (để giữ nguyên UserId và CreatedDate)
                    var oldData = await _context.Announcements.AsNoTracking().FirstOrDefaultAsync(x => x.AnnouncementId == id);

                    if (oldData != null)
                    {
                        announcement.UserId = oldData.UserId;
                        announcement.CreatedDate = oldData.CreatedDate; // Giữ ngày tạo gốc

                        _context.Update(announcement);
                        await _context.SaveChangesAsync();
                        TempData["Success"] = "Cập nhật thành công!";
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnnouncementExists(announcement.AnnouncementId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(announcement);
        }

        // 6. Xóa
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                _context.Announcements.Remove(announcement);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa thông báo.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Action này dùng để Sinh viên/Giảng viên xem chi tiết
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _context.Announcements
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.AnnouncementId == id);

            if (announcement == null) return NotFound();

            return View(announcement);
        }

        private bool AnnouncementExists(int id)
        {
            return _context.Announcements.Any(e => e.AnnouncementId == id);
        }
    }
}