using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // Thêm thư viện này từ code bạn kia

namespace StudentPortal.Controllers
{
    // [Authorize] // Bạn có thể bỏ comment dòng này nếu muốn bắt buộc đăng nhập cho cả file
    public class AnnouncementsController : Controller
    {
        private readonly StudentPortalContext _context;
        private readonly UserManager<User> _userManager;

        public AnnouncementsController(StudentPortalContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        // PHẦN 1: CODE CỦA BẠN (GIẢNG VIÊN & SINH VIÊN) - GIỮ NGUYÊN
        // ============================================================

        public async Task<IActionResult> Index()
        {
            var announcementsQuery = _context.Announcements.Include(a => a.User);
            var listAnnouncements = await announcementsQuery.ToListAsync(); // [cite: 5]

            if (User.IsInRole("Student"))
            {
                return RedirectToAction(nameof(StudentAnnouncement)); // [cite: 6]
            }
            else if (User.IsInRole("Lecturer"))
            {
                return RedirectToAction(nameof(LecturerAnnouncement)); // [cite: 7]
            }
            else if (User.IsInRole("Admin"))
            {
                // Logic cũ của bạn trỏ về View AdminIndex
                return View("AdminIndex", listAnnouncements); // [cite: 8]
            }
            return View(listAnnouncements); // [cite: 9]
        }

        // Đừng quên using StudentPortal.Models;
        public async Task<IActionResult> StudentAnnouncement(string searchString, int? pageNumber) // [cite: 10]
        {
            ViewData["CurrentFilter"] = searchString;
            var announcementsQuery = _context.Announcements
                                             .Include(a => a.User)
                                             .Where(a => a.Taker == RecipientType.Student 
                                             || a.Taker == RecipientType.All) // [cite: 12]
                                             .AsNoTracking();
            if (!string.IsNullOrEmpty(searchString))
            {
                announcementsQuery = announcementsQuery.Where(s => s.Title.Contains(searchString)
                                                                || s.Summary.Contains(searchString)); // [cite: 14]
            }

            announcementsQuery = announcementsQuery.OrderByDescending(a => a.CreatedDate);
            int pageSize = 20; // [cite: 15]
            var pagedData = await PaginatedList<Announcement>.CreateAsync(announcementsQuery, pageNumber ?? 1, pageSize);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") // [cite: 16]
            {
                return PartialView("_AnnouncementTable", pagedData);
            }

            return View(pagedData); // [cite: 18]
        }

        public async Task<IActionResult> LecturerAnnouncement(string searchString, int? pageNumber, bool showMyOnly = false)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["ShowMyOnly"] = showMyOnly; // Lưu trạng thái để View biết [cite: 19]

            var announcementsQuery = _context.Announcements
                                             .Include(a => a.User)
                                             .Where(a => a.Taker == RecipientType.Lecturer || a.Taker == RecipientType.All) // [cite: 20]
                                             .AsNoTracking();
            // --- LOGIC MỚI: Lọc thông báo của tôi ---
            if (showMyOnly)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                announcementsQuery = announcementsQuery.Where(a => a.UserId == currentUser.Id); // [cite: 22]
            }
            // ----------------------------------------

            if (!string.IsNullOrEmpty(searchString))
            {
                announcementsQuery = announcementsQuery.Where(s => s.Title.Contains(searchString)
                                                             || s.Summary.Contains(searchString)); // [cite: 23]
            }

            announcementsQuery = announcementsQuery.OrderByDescending(a => a.CreatedDate);
            int pageSize = 20; // [cite: 25]
            var pagedData = await PaginatedList<Announcement>.CreateAsync(announcementsQuery, pageNumber ?? 1, pageSize);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") // [cite: 26]
            {
                return PartialView("_AnnouncementTable", pagedData);
            }

            return View(pagedData); // [cite: 28]
        }


        // GET: Announcements/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound(); // [cite: 29]
            }

            var announcement = await _context.Announcements
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.AnnouncementId == id);
            if (announcement == null) // [cite: 30]
            {
                return NotFound();
            }

            return View(announcement); // [cite: 32]
        }

        // GET: Announcements/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") // [cite: 33]
            {
                return PartialView("Create");
            }

            return View(); // [cite: 35]
        }

        // POST: Announcements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement announcement)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != null) // [cite: 36]
            {
                announcement.UserId = int.Parse(currentUserId);
            }
            else
            {
                announcement.UserId = 1; // [cite: 38]
            }

            if (announcement.CreatedDate == default) announcement.CreatedDate = DateTime.Now;

            ModelState.Remove("UserId");
            ModelState.Remove("User"); // [cite: 39]

            if (ModelState.IsValid)
            {
                _context.Add(announcement);
                await _context.SaveChangesAsync(); // [cite: 40]

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true });
                }

                return RedirectToAction(nameof(Index)); // [cite: 42]
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Create", announcement); // [cite: 43]
            }

            return View(announcement);
        }

        // GET: Announcements/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound(); // [cite: 45]
            }

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) // [cite: 46]
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", announcement.UserId);
            return View(announcement); // [cite: 48]
        }

        // POST: Announcements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AnnouncementId,Title,Summary,Content,CreatedDate,ExpiredDate,Taker,UserId")] Announcement announcement)
        {
            if (id != announcement.AnnouncementId)
            {
                return NotFound(); // [cite: 50]
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(announcement);
                    await _context.SaveChangesAsync(); // [cite: 51]
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnnouncementExists(announcement.AnnouncementId))
                    {
                        return NotFound(); // [cite: 52]
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index)); // [cite: 54]
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", announcement.UserId);
            return View(announcement); // [cite: 55]
        }

        // GET: Announcements/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound(); // [cite: 56]
            }

            var announcement = await _context.Announcements
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.AnnouncementId == id);
            if (announcement == null) // [cite: 57]
            {
                return NotFound();
            }

            return View(announcement); // [cite: 59]
        }

        // POST: Announcements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null) // [cite: 60]
            {
                _context.Announcements.Remove(announcement);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index)); // [cite: 62]
        }

        // --- Ajax Delete (Phần của bạn) ---
        [HttpPost]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) // [cite: 63]
            {
                return Json(new { success = false, message = "Không tìm thấy thông báo này!" });
            }

            var currentUserId = _userManager.GetUserId(User);
            if (announcement.UserId.ToString() != currentUserId && !User.IsInRole("Admin"))
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa bài viết của người khác!" }); // [cite: 66]
            }

            _context.Announcements.Remove(announcement);
            await _context.SaveChangesAsync(); // [cite: 67]

            return Json(new { success = true, message = "Đã xóa thành công!" }); // [cite: 68]
        }

        private bool AnnouncementExists(int id)
        {
            return _context.Announcements.Any(e => e.AnnouncementId == id); // [cite: 69]
        }


        // ============================================================
        // PHẦN 2: CODE CỦA BẠN KIA (DÀNH CHO ADMIN)
        // (Tôi đã đổi tên hàm thành Admin... để không bị trùng với phần trên)
        // ============================================================

        // 1. Admin Index (Thay thế cho Index của bạn kia) [cite: 73]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var announcements = await _context.Announcements
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();
            return View("AdminIndex", announcements); // Đảm bảo bạn có View tên là AdminIndex.cshtml
        }

        // 2. Admin Create - GET (Thay thế Create cũ) [cite: 75]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminCreate()
        {
            return View("AdminCreate"); // Bạn cần tạo View AdminCreate.cshtml hoặc dùng chung view Create
        }

        // 3. Admin Create - POST [cite: 76-82]
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminCreate(Announcement announcement)
        {
            ModelState.Remove("User");
            ModelState.Remove("UserId"); // [cite: 76]

            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null) // [cite: 77]
                {
                    announcement.UserId = currentUser.Id; // [cite: 78]
                    announcement.CreatedDate = DateTime.Now; // [cite: 79]

                    _context.Add(announcement);
                    await _context.SaveChangesAsync(); // [cite: 80]

                    TempData["Success"] = "Đã đăng thông báo thành công!";
                    return RedirectToAction(nameof(AdminIndex)); // Quay về trang AdminIndex
                }
            }
            return View("AdminCreate", announcement);
        }

        // 4. Admin Edit - GET [cite: 83]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminEdit(int? id)
        {
            if (id == null) return NotFound();
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();

            return View("AdminEdit", announcement); // Cần view AdminEdit.cshtml
        }

        // 5. Admin Edit - POST [cite: 85-92]
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminEdit(int id, Announcement announcement)
        {
            if (id != announcement.AnnouncementId) return NotFound();

            ModelState.Remove("User");
            ModelState.Remove("UserId"); // [cite: 86]

            if (ModelState.IsValid)
            {
                try
                {
                    var oldData = await _context.Announcements.AsNoTracking().FirstOrDefaultAsync(x => x.AnnouncementId == id); // [cite: 87]

                    if (oldData != null)
                    {
                        announcement.UserId = oldData.UserId;
                        announcement.CreatedDate = oldData.CreatedDate; // Giữ ngày tạo gốc [cite: 88]

                        _context.Update(announcement);
                        await _context.SaveChangesAsync(); // [cite: 89]
                        TempData["Success"] = "Cập nhật thành công!";
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnnouncementExists(announcement.AnnouncementId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(AdminIndex));
            }
            return View("AdminEdit", announcement);
        }

        // 6. Admin Delete - POST [cite: 93-95]
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminDelete(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null) // [cite: 93]
            {
                _context.Announcements.Remove(announcement);
                await _context.SaveChangesAsync(); // [cite: 94]
                TempData["Success"] = "Đã xóa thông báo.";
            }
            return RedirectToAction(nameof(AdminIndex));
        }
    }
}