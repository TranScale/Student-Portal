using Microsoft.AspNetCore.Mvc;
using StudentPortal.Models;
using StudentPortal.Business.Interface;

namespace StudentPortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleController : ControllerBase
    {
        private readonly IScheduleService _scheduleService;

        public ScheduleController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSchedule([FromBody] ScheduleItem scheduleItem)
        {
            if (scheduleItem == null)
                return BadRequest("Schedule item is required");

            try
            {
                await _scheduleService.CreateSchedule(scheduleItem);
                return CreatedAtAction(nameof(GetScheduleBySection),
                    new { sectionId = scheduleItem.CourseSectionId }, scheduleItem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating schedule: {ex.Message}");
            }
        }

        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<List<ScheduleItem>>> GetScheduleByStudent(int studentId)
        {
            try
            {
                var schedule = await _scheduleService.GetScheduleByStudent(studentId);
                return Ok(schedule);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving schedule: {ex.Message}");
            }
        }

        [HttpGet("lecturer/{lecturerId}")]
        public async Task<ActionResult<List<ScheduleItem>>> GetScheduleByLecturer(int lecturerId)
        {
            try
            {
                var schedule = await _scheduleService.GetScheduleByLecturer(lecturerId);
                return Ok(schedule);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving schedule: {ex.Message}");
            }
        }

        [HttpGet("section/{sectionId}")]
        public async Task<ActionResult<List<ScheduleItem>>> GetScheduleBySection(int sectionId)
        {
            try
            {
                var schedule = await _scheduleService.GetScheduleBySection(sectionId);
                return Ok(schedule);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving schedule: {ex.Message}");
            }
        }
    }
}