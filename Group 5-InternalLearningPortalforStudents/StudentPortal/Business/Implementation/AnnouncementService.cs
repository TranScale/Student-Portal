using StudentPortal.Business.Interface;
using StudentPortal.Models;
using StudentPortal.Data_Access.Repository.Implementation;

namespace StudentPortal.Business.Implementation
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly Repository<Announcement> _repo;
        public AnnouncementService(Repository<Announcement> repo)
        {
            _repo = repo;
        }

        public async Task<bool> CreateAnnouncement(Announcement announcement)
        {
            try
            {
                await _repo.Add(announcement);
                return true;
            }
            catch { return false; }
        }


        public async Task<bool> UpdateAnnouncement(Announcement announcement)
        {
            try
            {
                await _repo.Update(announcement);
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteAnnouncement(int id)
        {
            try
            {
                await _repo.Delete(id);
                return true;
            }
            catch { return false; }
        }

        public async Task<IEnumerable<Announcement>> GetAllAnnouncements()
        {
            return await _repo.GetAll();
        }

        public async Task<IEnumerable<Announcement>> GetAnnouncementsForUser(RecipientType user)
        {
            var all = await _repo.GetAll();
            return all.Where(a => a.Taker == user);
        }

        public async Task<Announcement?> GetById(int id)
        {
            return await _repo.GetById(id);
        }
    }
}
