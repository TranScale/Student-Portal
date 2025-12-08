using StudentPortal.Models;
namespace StudentPortal.Business.Interface
{
    public interface IAnnouncementService
    {
        Task<bool> CreateAnnouncement(Announcement announcement);
        Task<bool> UpdateAnnouncement(Announcement announcement);
        Task<bool> DeleteAnnouncement(int id);

        Task<IEnumerable<Announcement>> GetAllAnnouncements();
        Task<IEnumerable<Announcement>> GetAnnouncementsForUser(RecipientType user);
        Task<Announcement?> GetById(int id);
    }
}
