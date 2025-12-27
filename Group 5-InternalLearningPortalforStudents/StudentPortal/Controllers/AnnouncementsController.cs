using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudentPortal.Business.Interface;
using StudentPortal.Data; // Cần dùng để lấy danh sách User cho Admin chọn (nếu cần)
using StudentPortal.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore; // xiu xoa'
namespace StudentPortal.Controllers
{
    [Authorize] // Yêu cầu đăng nhập mới được truy cập
    public class AnnouncementsController : Controller
    {
        private readonly IAnnouncementService _announcementService;
        private readonly StudentPortalContext _context; // Dùng để load dropdown cho Admin

        public AnnouncementsController(IAnnouncementService announcementService, StudentPortalContext context)
        {
            _announcementService = announcementService;
            _context = context;
        }

        // GET: Announcements
        public async Task<IActionResult> Index()
        {
            var allAnnouncements = await _announcementService.GetAllAnnouncements();
            var userId = GetCurrentUserId();

            // 1. ADMIN: Xem toàn bộ để quản lý
            if (User.IsInRole("Admin"))
            {
                return View(allAnnouncements);
            }

            // 2. GIẢNG VIÊN: Xem các thông báo do MÌNH tạo ra (để Edit/Delete)
            if (User.IsInRole("Lecturer"))
            {
                // Lọc bài đăng của chính giảng viên này
                var myAnnouncements = allAnnouncements.Where(a => a.UserId == userId);
                return View(myAnnouncements);
            }

            // 3. SINH VIÊN: Chỉ xem thông báo dành cho SV hoặc Toàn trường
            if (User.IsInRole("Student"))
            {
                var studentAnnouncements = allAnnouncements.Where(a =>
                    a.Taker == RecipientType.Student ||
                    a.Taker == RecipientType.All
                );
                return View(studentAnnouncements);
            }

            // Mặc định trả về rỗng nếu không khớp role
            return View(new List<Announcement>());
        }

        // GET: Announcements/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _announcementService.GetById(id.Value);
            if (announcement == null) return NotFound();

            // Logic bảo mật: Sinh viên không được xem thông báo dành riêng cho Giảng viên
            if (User.IsInRole("Student") && announcement.Taker == RecipientType.Lecturer)
            {
                return Forbid();
            }

            return View(announcement);
        }

        // GET: Announcements/Create
        // Chỉ Admin và Giảng viên mới được tạo
        [Authorize(Roles = "Admin,Lecturer")]
        public IActionResult Create()
        {
            // Nếu là Admin, cho phép chọn người đăng (UserId)
            if (User.IsInRole("Admin"))
            {
                ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName");
            }
            // Giảng viên không cần chọn UserId (tự lấy ID của họ)

            return View();
        }

        // POST: Announcements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Create([Bind("AnnouncementId,Title,Summary,Content,CreatedDate,ExpiredDate,Taker,UserId")] Announcement announcement)
        {
            // Tự động gán ngày tạo
            announcement.CreatedDate = DateTime.Now;

            // XỬ LÝ NGƯỜI ĐĂNG (OWNERSHIP)
            if (User.IsInRole("Lecturer"))
            {
                // Giảng viên đăng: Bắt buộc UserId là chính họ
                announcement.UserId = GetCurrentUserId();

                // Xóa lỗi validate UserId vì ta vừa gán code-behind
                ModelState.Remove("UserId");
                ModelState.Remove("User");
            }
            else if (User.IsInRole("Admin"))
            {
                // Admin: Nếu không chọn ai, tự gán là chính Admin
                if (announcement.UserId == 0)
                {
                    announcement.UserId = GetCurrentUserId();
                    ModelState.Remove("UserId");
                    ModelState.Remove("User");
                }
            }

            if (ModelState.IsValid)
            {
                await _announcementService.CreateAnnouncement(announcement);
                return RedirectToAction(nameof(Index));
            }

            // Nếu lỗi, load lại view
            if (User.IsInRole("Admin"))
            {
                ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", announcement.UserId);
            }
            return View(announcement);
        }

        // GET: Announcements/Edit/5
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _announcementService.GetById(id.Value);
            if (announcement == null) return NotFound();

            // KIỂM TRA QUYỀN SỞ HỮU
            // Giảng viên chỉ được sửa bài của chính mình
            if (User.IsInRole("Lecturer") && announcement.UserId != GetCurrentUserId())
            {
                return Forbid(); // Trả về lỗi 403
            }

            if (User.IsInRole("Admin"))
            {
                ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", announcement.UserId);
            }
            return View(announcement);
        }

        // POST: Announcements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Edit(int id, Announcement announcement)
        {
            if (id != announcement.AnnouncementId) return NotFound();

            // Kiểm tra quyền sở hữu khi POST
            if (User.IsInRole("Lecturer"))
            {
                var original = await _announcementService.GetById(id);
                if (original == null || original.UserId != GetCurrentUserId())
                {
                    return Forbid();
                }

                // Bảo EF Core: "Quên thằng original này đi, để tôi chuẩn bị lưu thằng mới"
                _context.Entry(original).State = EntityState.Detached;

                // Đảm bảo giảng viên không thể đổi người đăng sang người khác
                announcement.UserId = original.UserId;

                // Xóa các lỗi validate không cần thiết
                ModelState.Remove("UserId");
                ModelState.Remove("User");
            }

            if (ModelState.IsValid)
            {
                await _announcementService.UpdateAnnouncement(announcement);
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Admin"))
            {
                ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", announcement.UserId);
            }
            return View(announcement);
        }

        // GET: Announcements/Delete/5
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _announcementService.GetById(id.Value);
            if (announcement == null) return NotFound();

            // Giảng viên chỉ được xóa bài của mình
            if (User.IsInRole("Lecturer") && announcement.UserId != GetCurrentUserId())
            {
                return Forbid();
            }

            return View(announcement);
        }

        // POST: Announcements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Cần kiểm tra quyền lần nữa trước khi xóa thật
            var announcement = await _announcementService.GetById(id);
            if (announcement == null) return NotFound();

            if (User.IsInRole("Lecturer") && announcement.UserId != GetCurrentUserId())
            {
                return Forbid();
            }

            await _announcementService.DeleteAnnouncement(id);
            return RedirectToAction(nameof(Index));
        }

        // Helper: Lấy ID người dùng đang đăng nhập
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier); // Tìm Claim chứa ID
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0; // Hoặc xử lý lỗi nếu không tìm thấy ID
        }




        // Action tạm thời để tạo User test
        // Truy cập bằng đường dẫn: /Announcements/CreateTestUser
        [AllowAnonymous]
        public async Task<IActionResult> CreateTestUser()
        {
            // 1. Kiểm tra xem đã có user nào chưa để tránh tạo trùng
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "gv@test.com");
            if (existingUser != null)
            {
                return Content($"Đã có User test rồi! ID của họ là: {existingUser.UserId}. Bạn hãy dùng ID này.");
            }

            // 2. Tạo User mới (Giả sử Role 1 là Lecturer)
            var newUser = new User
            {
                FullName = "Giảng Viên Test",
                Email = "gv@test.com",
                Password = "123456", // Mật khẩu demo
                UserRole = UserRoles.Lecturer, // Đảm bảo bạn đã using StudentPortal.Models;
                                           // Các trường khác nếu bắt buộc thì thêm vào đây
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync(); // Lúc này ID sẽ được sinh ra

            // 3. (Tùy chọn) Nếu hệ thống yêu cầu bảng Lecturer phải có dữ liệu
            // var newLecturer = new Lecturer { UserId = newUser.UserId, FacultyId = 1 };
            // _context.Lecturers.Add(newLecturer);
            // await _context.SaveChangesAsync();

            return Content($"Đã tạo thành công! ID mới là: {newUser.UserId}. Hãy nhớ số này!");
        }
    }
}