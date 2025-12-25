using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;
using StudentPortal.Business.Interface;
using StudentPortal.Data_Access.Repository.Interface;

namespace StudentPortal.Business.Implementation 
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly IRepository<Announcement> _repo;
        private readonly StudentPortalContext _context;

        // FIX: Dùng IRepository<Announcement>, không dùng Repository<Announcement>
        public AnnouncementService(
            IRepository<Announcement> repo,
            StudentPortalContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<bool> CreateAnnouncement(Announcement announcement)
        {
            try
            {
                announcement.CreatedDate = DateTime.Now;
                await _repo.Add(announcement);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateAnnouncement(Announcement announcement)
        {
            try
            {
                var existing = await _context.Announcements
                    .FirstOrDefaultAsync(a => a.AnnouncementId == announcement.AnnouncementId);

                if (existing == null) return false;

                existing.Title = announcement.Title;
                existing.Summary = announcement.Summary;
                existing.Content = announcement.Content;
                existing.Taker = announcement.Taker;
                existing.ExpiredDate = announcement.ExpiredDate;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAnnouncement(int id)
        {
            try
            {
                await _repo.Delete(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<Announcement>> GetAllAnnouncements()
        {
            return await _context.Announcements
                .Include(a => a.User)
                .Where(a => a.ExpiredDate == null || a.ExpiredDate > DateTime.Now)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Announcement>> GetAnnouncementsForUser(RecipientType userType)
        {
            return await _context.Announcements
                .Include(a => a.User)
                .Where(a => a.Taker == userType
                    && (a.ExpiredDate == null || a.ExpiredDate > DateTime.Now))
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();
        }

        public async Task<Announcement?> GetById(int id)
        {
            return await _context.Announcements
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AnnouncementId == id
                    && (a.ExpiredDate == null || a.ExpiredDate > DateTime.Now));
        }
    }
}