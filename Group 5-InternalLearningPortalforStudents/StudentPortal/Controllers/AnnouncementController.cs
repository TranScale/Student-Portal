using Microsoft.AspNetCore.Mvc;
using StudentPortal.Business.Interface;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnnouncementController : ControllerBase
    {
        private readonly IAnnouncementService _announcementService;

        public AnnouncementController(IAnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement([FromBody] Announcement announcement)
        {
            if (announcement == null)
                return BadRequest("Announcement data is required");

            try
            {
                var success = await _announcementService.CreateAnnouncement(announcement);

                if (!success)
                    return StatusCode(500, "Failed to create announcement");

                return Ok(new { message = "Announcement created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating announcement: {ex.Message}");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAnnouncement([FromBody] Announcement announcement)
        {
            if (announcement == null)
                return BadRequest("Announcement data is required");

            try
            {
                var success = await _announcementService.UpdateAnnouncement(announcement);

                if (!success)
                    return NotFound("Announcement not found or update failed");

                return Ok(new { message = "Announcement updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating announcement: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            try
            {
                var success = await _announcementService.DeleteAnnouncement(id);

                if (!success)
                    return NotFound($"Announcement with ID {id} not found");

                return Ok(new { message = "Announcement deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting announcement: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Announcement>>> GetAllAnnouncements()
        {
            try
            {
                var announcements = await _announcementService.GetAllAnnouncements();
                return Ok(announcements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving announcements: {ex.Message}");
            }
        }

        [HttpGet("user-type/{userType}")]
        public async Task<ActionResult<IEnumerable<Announcement>>> GetAnnouncementsForUser(RecipientType userType)
        {
            try
            {
                var announcements = await _announcementService.GetAnnouncementsForUser(userType);
                return Ok(announcements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving announcements: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Announcement>> GetAnnouncementById(int id)
        {
            try
            {
                var announcement = await _announcementService.GetById(id);

                if (announcement == null)
                    return NotFound($"Announcement with ID {id} not found or has expired");

                return Ok(announcement);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving announcement: {ex.Message}");
            }
        }
    }
}