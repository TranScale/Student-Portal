using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentPortal.Business.Interface; // Namespace chứa Interface Service
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    [Authorize]
    public class AnnouncementsController : Controller
    {
        private readonly IAnnouncementService _announcementService; // Gọi Service
        private readonly UserManager<User> _userManager;

        public AnnouncementsController(IAnnouncementService announcementService, UserManager<User> userManager)
        {
            _announcementService = announcementService;
            _userManager = userManager;
        }

        // 1. Danh sách thông báo
        public async Task<IActionResult> Index()
        {
            IEnumerable<Announcement> listToShow;

            if (User.IsInRole("Student"))
            {
                // Logic: Lấy thông báo dành cho Student
                // (Lưu ý: Service GetAnnouncementsForUser chỉ lọc chính xác theo Type, 
                // nhưng thực tế SV cần xem cả 'Student' VÀ 'All'. 
                // Nên ta lấy GetAll rồi lọc LINQ tại đây sẽ linh hoạt hơn, hoặc sửa Service)

                var all = await _announcementService.GetAllAnnouncements();
                listToShow = all.Where(a => a.Taker == RecipientType.All || a.Taker == RecipientType.Student);
            }
            else if (User.IsInRole("Lecturer"))
            {
                var user = await _userManager.GetUserAsync(User);
                var all = await _announcementService.GetAllAnnouncements();

                // GV xem: All, Lecturer, hoặc bài của chính mình đăng
                listToShow = all.Where(a =>
                    a.Taker == RecipientType.All ||
                    a.Taker == RecipientType.Lecturer ||
                    a.UserId == user.Id);
            }
            else // Admin
            {
                listToShow = await _announcementService.GetAllAnnouncements();
            }

            return View(listToShow.OrderByDescending(a => a.CreatedDate));
        }

        // 2. Tạo - GET
        [Authorize(Roles = "Admin, Lecturer")]
        public IActionResult Create()
        {
            return View();
        }

        // 3. Tạo - POST (Dùng Service)
        [HttpPost]
        //[Authorize(Roles = "Admin, Lecturer")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement model)
        {
            var user = await _userManager.GetUserAsync(User);

            // Set dữ liệu hệ thống
            model.UserId = user.Id;
            model.CreatedDate = DateTime.Now;

            // Bỏ qua validate User vì ta tự gán
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                // Gọi Service tạo mới
                bool result = await _announcementService.CreateAnnouncement(model);

                if (result)
                {
                    TempData["Success"] = "Đăng thông báo thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi hệ thống khi lưu thông báo.");
                }
            }
            return View(model);
        }

        // 4. Delete (Dùng Service)
        [HttpPost]
        //[Authorize(Roles = "Admin, Lecturer")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Lấy info để check quyền trước
            var announcement = await _announcementService.GetById(id);
            if (announcement == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            // Logic: Admin xóa tất cả, GV chỉ xóa bài mình
            if (User.IsInRole("Admin") || announcement.UserId == user.Id)
            {
                await _announcementService.DeleteAnnouncement(id);
                TempData["Success"] = "Đã xóa thông báo.";
            }
            else
            {
                TempData["Error"] = "Bạn không có quyền xóa thông báo này.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}