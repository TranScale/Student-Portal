using Microsoft.AspNetCore.Mvc;
using StudentPortal.Business.Interface;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentController(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        [HttpPost("enroll/{studentId}/{sectionId}")]
        public async Task<IActionResult> EnrollStudent(int studentId, int sectionId)
        {
            try
            {
                await _enrollmentService.EnrollStudent(studentId, sectionId);
                return Ok(new { message = "Enrollment request submitted successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error enrolling student: {ex.Message}");
            }
        }

        [HttpDelete("unenroll/{studentId}/{sectionId}")]
        public async Task<IActionResult> UnEnrollStudent(int studentId, int sectionId)
        {
            try
            {
                await _enrollmentService.UnEnrollStudent(studentId, sectionId);
                return Ok(new { message = "Unenrolled successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error unenrolling student: {ex.Message}");
            }
        }

        [HttpPut("approve/{enrollmentId}")]
        public async Task<IActionResult> ApproveEnrollment(int enrollmentId)
        {
            try
            {
                await _enrollmentService.ApproveEnrollment(enrollmentId);
                return Ok(new { message = "Enrollment approved successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error approving enrollment: {ex.Message}");
            }
        }

        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<List<Enrollment>>> GetEnrollmentsByStudent(int studentId)
        {
            try
            {
                var enrollments = await _enrollmentService.GetEnrollmentsByStudent(studentId);
                return Ok(enrollments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving enrollments: {ex.Message}");
            }
        }

        [HttpGet("section/{sectionId}")]
        public async Task<ActionResult<List<Enrollment>>> GetEnrollmentsBySection(int sectionId)
        {
            try
            {
                var enrollments = await _enrollmentService.GetEnrollmentsBySection(sectionId);
                return Ok(enrollments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving enrollments: {ex.Message}");
            }
        }
    }
}