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

namespace StudentPortal.Controllers
{
    public class AnnouncementsController : Controller
    {
        private readonly StudentPortalContext _context;
        private readonly UserManager<User> _userManager;

        public AnnouncementsController(StudentPortalContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var announcementsQuery = _context.Announcements.Include(a => a.User);
            var listAnnouncements = await announcementsQuery.ToListAsync();

            if (User.IsInRole("Student"))
            {
                return RedirectToAction(nameof(StudentAnnouncement));
            }
            else if (User.IsInRole("Lecturer"))
            {
                return RedirectToAction(nameof(LecturerAnnouncement));
            }
            else if (User.IsInRole("Admin"))
            {
                return View("AdminIndex", listAnnouncements);
            }
            return View(listAnnouncements);
        }

        // Đừng quên using StudentPortal.Models;

        public async Task<IActionResult> StudentAnnouncement(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            var announcementsQuery = _context.Announcements
                                             .Include(a => a.User)
                                             .Where(a => a.Taker == RecipientType.Student || a.Taker == RecipientType.All)
                                             .AsNoTracking();

            if (!string.IsNullOrEmpty(searchString))
            {
                announcementsQuery = announcementsQuery.Where(s => s.Title.Contains(searchString)
                                                                || s.Summary.Contains(searchString));
            }

            announcementsQuery = announcementsQuery.OrderByDescending(a => a.CreatedDate);

            int pageSize = 20;
            var pagedData = await PaginatedList<Announcement>.CreateAsync(announcementsQuery, pageNumber ?? 1, pageSize);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_AnnouncementTable", pagedData);
            }

            return View(pagedData);
        }

        public async Task<IActionResult> LecturerAnnouncement(string searchString, int? pageNumber, bool showMyOnly = false)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["ShowMyOnly"] = showMyOnly; // Lưu trạng thái để View biết

            var announcementsQuery = _context.Announcements
                                             .Include(a => a.User)
                                             .Where(a => a.Taker == RecipientType.Lecturer || a.Taker == RecipientType.All)
                                             .AsNoTracking();

            // --- LOGIC MỚI: Lọc thông báo của tôi ---
            if (showMyOnly)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                announcementsQuery = announcementsQuery.Where(a => a.UserId == currentUser.Id);
            }
            // ----------------------------------------

            if (!string.IsNullOrEmpty(searchString))
            {
                announcementsQuery = announcementsQuery.Where(s => s.Title.Contains(searchString)
                                                                || s.Summary.Contains(searchString));
            }

            announcementsQuery = announcementsQuery.OrderByDescending(a => a.CreatedDate);

            int pageSize = 20;
            var pagedData = await PaginatedList<Announcement>.CreateAsync(announcementsQuery, pageNumber ?? 1, pageSize);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_AnnouncementTable", pagedData);
            }

            return View(pagedData);
        }


        // GET: Announcements/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.AnnouncementId == id);
            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        // GET: Announcements/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id"); 

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Create");
            }

            return View();
        }

        // POST: Announcements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement announcement)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserId != null)
            {
                announcement.UserId = int.Parse(currentUserId);
            }
            else
            {
                announcement.UserId = 1;
            }

            if (announcement.CreatedDate == default) announcement.CreatedDate = DateTime.Now;

            ModelState.Remove("UserId");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                _context.Add(announcement);
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true });
                }

                return RedirectToAction(nameof(Index));
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Create", announcement);
            }

            return View(announcement);
        }

        // GET: Announcements/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", announcement.UserId);
            return View(announcement);
        }

        // POST: Announcements/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AnnouncementId,Title,Summary,Content,CreatedDate,ExpiredDate,Taker,UserId")] Announcement announcement)
        {
            if (id != announcement.AnnouncementId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(announcement);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnnouncementExists(announcement.AnnouncementId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", announcement.UserId);
            return View(announcement);
        }

        // GET: Announcements/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.AnnouncementId == id);
            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        // POST: Announcements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                _context.Announcements.Remove(announcement);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // --- THÊM ĐOẠN NÀY VÀO CUỐI CONTROLLER ---

        [HttpPost]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            // 1. Tìm bài viết
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông báo này!" });
            }

            // 2. Kiểm tra quyền (Chính chủ hoặc Admin mới được xóa)
            // Lấy ID người đang đăng nhập
            var currentUserId = _userManager.GetUserId(User);

            // So sánh: Nếu ID người tạo bài KHÁC ID người đang nhập VÀ không phải Admin -> Chặn
            // Lưu ý: announcement.UserId là int, currentUserId là string nên phải chuyển đổi để so sánh
            if (announcement.UserId.ToString() != currentUserId && !User.IsInRole("Admin"))
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa bài viết của người khác!" });
            }

            // 3. Xóa và Lưu
            _context.Announcements.Remove(announcement);
            await _context.SaveChangesAsync();

            // 4. Trả về thành công
            return Json(new { success = true, message = "Đã xóa thành công!" });
        }


        private bool AnnouncementExists(int id)
        {
            return _context.Announcements.Any(e => e.AnnouncementId == id);
        }
    }
}
