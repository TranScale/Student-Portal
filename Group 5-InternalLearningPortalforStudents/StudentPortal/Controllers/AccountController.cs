using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;
using System.Security.Claims;

namespace StudentPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly StudentPortalContext _context;

        public AccountController(StudentPortalContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult LoginDemo()
        {
            return Content("Link đăng nhập:\n- Giảng viên: /Account/QuickLogin?email=gv@test.com\n- Admin: /Account/QuickLogin?email=admin@test.com");
        }

        [HttpGet]
        public async Task<IActionResult> QuickLogin(string email)
        {
            if (string.IsNullOrEmpty(email)) return Content("Vui lòng nhập email vào đường dẫn.");

            // 1. Tìm User
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return Content($"Lỗi: Không tìm thấy user '{email}'. Hãy chạy /Announcements/CreateTestUser trước.");
            }

            // 2. Tạo Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName ?? "Unknown"),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), 
                
                // --- SỬA LẠI ĐÚNG TÊN THUỘC TÍNH CỦA BẠN Ở ĐÂY ---
                // Dùng user.UserRole thay vì user.Role
                new Claim(ClaimTypes.Role, user.UserRole.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Announcements");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Content("Đã đăng xuất.");
        }
    }
}