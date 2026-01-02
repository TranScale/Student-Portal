using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    [Authorize] // bắt buộc login
    public class CertificatesController : Controller
    {
        private readonly StudentPortalContext _context;

        public CertificatesController(StudentPortalContext context)
        {
            _context = context;
        }

        // lấy UserId từ email đăng nhập (Hello sv01@sp.com)
        private async Task<int> GetCurrentUserIdAsync()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return 0;

            return await _context.Users
                .Where(u => u.Email == email)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
        }

        // =========================
        // INDEX: Admin/Lecturer xem tất cả
        //       Student chỉ xem của mình
        // =========================
        [Authorize(Roles = "Admin,Student,Lecturer")]
        public async Task<IActionResult> Index()
        {
            var query = _context.Certificates.Include(c => c.User).AsQueryable();

            if (User.IsInRole("Student"))
            {
                var uid = await GetCurrentUserIdAsync();
                query = query.Where(c => c.UserId == uid);
            }

            return View(await query.ToListAsync());
        }

        // =========================
        // DETAILS: Admin/Lecturer xem được
        //          Student chỉ xem của mình
        // =========================
        [Authorize(Roles = "Admin,Student,Lecturer")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var certificate = await _context.Certificates
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.CertificateId == id);

            if (certificate == null) return NotFound();

            if (User.IsInRole("Student"))
            {
                var uid = await GetCurrentUserIdAsync();
                if (certificate.UserId != uid) return Forbid();
            }

            return View(certificate);
        }

        // =========================
        // CREATE: Admin + Student tạo được
        // Lecturer không tạo
        // =========================
        [Authorize(Roles = "Admin,Student")]
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Admin"))
            {
                ViewData["UserId"] = new SelectList(await _context.Users.ToListAsync(), "UserId", "UserId");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Student")]
        public async Task<IActionResult> Create([Bind("CertificateName,CertificateType,IssuedBy,IssuedDate,ExpiredDate,Score,FilePath,UserId")] Certificate certificate)
        {
            // Student: ép UserId = chính nó
            if (User.IsInRole("Student"))
            {
                var uid = await GetCurrentUserIdAsync();
                if (uid == 0) return Forbid();

                certificate.UserId = uid;
                ModelState.Remove(nameof(Certificate.UserId));
            }

            // Admin: bắt buộc chọn UserId
            if (User.IsInRole("Admin") && certificate.UserId <= 0)
            {
                ModelState.AddModelError(nameof(Certificate.UserId), "Vui lòng chọn UserId.");
            }

            if (!ModelState.IsValid)
            {
                if (User.IsInRole("Admin"))
                    ViewData["UserId"] = new SelectList(await _context.Users.ToListAsync(), "UserId", "UserId", certificate.UserId);

                return View(certificate);
            }

            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT/DELETE: chỉ Admin (CRUD full)
        // =========================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var certificate = await _context.Certificates.FindAsync(id);
            if (certificate == null) return NotFound();

            ViewData["UserId"] = new SelectList(await _context.Users.ToListAsync(), "UserId", "UserId", certificate.UserId);
            return View(certificate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("CertificateId,CertificateName,CertificateType,IssuedBy,IssuedDate,ExpiredDate,Score,FilePath,UserId")] Certificate certificate)
        {
            if (id != certificate.CertificateId) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["UserId"] = new SelectList(await _context.Users.ToListAsync(), "UserId", "UserId", certificate.UserId);
                return View(certificate);
            }

            _context.Update(certificate);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var certificate = await _context.Certificates
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.CertificateId == id);

            if (certificate == null) return NotFound();
            return View(certificate);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var certificate = await _context.Certificates.FindAsync(id);
            if (certificate != null)
                _context.Certificates.Remove(certificate);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
