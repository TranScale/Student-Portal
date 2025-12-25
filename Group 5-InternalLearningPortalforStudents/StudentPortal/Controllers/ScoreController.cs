using Microsoft.AspNetCore.Mvc;
using StudentPortal.Business.Interface;
using StudentPortal.Models;

namespace StudentPortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScoreController : ControllerBase
    {
        private readonly IScoreService _scoreService;

        public ScoreController(IScoreService scoreService)
        {
            _scoreService = scoreService;
        }

        [HttpPost]
        public async Task<ActionResult<Score>> AddScore([FromBody] Score score)
        {
            if (score == null)
                return BadRequest("Score data is required");

            try
            {
                var createdScore = await _scoreService.AddScore(score);
                return CreatedAtAction(nameof(GetScoresByStudent),
                    new { studentId = createdScore.StudentId }, createdScore);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding score: {ex.Message}");
            }
        }

        [HttpPut]
        public async Task<ActionResult<Score>> UpdateScore([FromBody] Score score)
        {
            if (score == null)
                return BadRequest("Score data is required");

            try
            {
                var updatedScore = await _scoreService.UpdateScore(score);
                return Ok(updatedScore);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating score: {ex.Message}");
            }
        }

        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<List<Score>>> GetScoresByStudent(int studentId)
        {
            try
            {
                var scores = await _scoreService.GetScoresByStudent(studentId);
                return Ok(scores);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving scores: {ex.Message}");
            }
        }

        [HttpGet("section/{sectionId}")]
        public async Task<ActionResult<List<Score>>> GetScoresBySection(int sectionId)
        {
            try
            {
                var scores = await _scoreService.GetScoresBySection(sectionId);
                return Ok(scores);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving scores: {ex.Message}");
            }
        }
    }
}