using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedbackApi.Data;
using StudentFeedbackApi.Models;
using StudentFeedbackApi.DTOs;
using System.Security.Claims;

namespace StudentFeedbackApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class FeedbackController : ControllerBase
    {
        private readonly AppDbContext _db;

        public FeedbackController(AppDbContext db)
        {
            _db = db;
        }

        // GET: api/feedback

        [HttpGet]
        public async Task<IActionResult> GetAllFeedback()
        {
            var feedbacks = await _db.Feedbacks
                .Include(f => f.User)
                .Include(f => f.Course)
                .Where(f => f.User.Role.ToLower() == "student")
                .Select(f => new
                {
                    f.F_Id,
                    f.CourseId,
                    f.UserId,
                    f.Rating,
                    f.Comments,
                    f.SubmittedAt,
                    Course = new
                    {
                        f.Course.CourseId,
                        f.Course.Course_Name
                    },
                    User = new
                    {
                        f.User.UserId,
                        f.User.Name
                    }
                })
                .ToListAsync();

            return Ok(feedbacks);
        }

        // GET: api/feedback/{id}

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetFeedbackById(int id)
        {
            var feedback = await _db.Feedbacks
                .Include(f => f.User)
                .Include(f => f.Course)
                .Select(f => new
                {
                    f.F_Id,
                    f.CourseId,
                    f.UserId,
                    f.Rating,
                    f.Comments,
                    f.SubmittedAt,
                    Course = new
                    {
                        f.Course.CourseId,
                        f.Course.Course_Name,
                        f.Course.Description,
                        f.Course.CreatedBy
                    },
                    User = new
                    {
                        f.User.UserId,
                        f.User.Name,
                        f.User.Email,
                        f.User.Role 
                    }
                })
                .FirstOrDefaultAsync(f => f.F_Id == id);

            if (feedback == null)
                return NotFound(new { message = $"Feedback with ID {id} not found." });

            return Ok(feedback);
        }

        // GET: api/feedback/my
        [HttpGet("my")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyFeedbackCourses()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new { message = "User ID not found in token." });

            int userId = int.Parse(userIdClaim.Value);

            var submittedCourseIds = await _db.Feedbacks
                .Where(f => f.UserId == userId)
                .Select(f => f.CourseId)
                .ToListAsync();

            return Ok(submittedCourseIds);
        }


        // POST: api/feedback
        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { message = "Validation Failed", errors });
            }

            // Extract userId from token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            int userId = int.Parse(userIdClaim.Value);

            var course = await _db.Courses.FindAsync(dto.CourseId);
            var user = await _db.Users.FindAsync(userId);

            if (course == null || user == null)
            {
                return BadRequest(new
                {
                    message = "Invalid CourseId or User.",
                    courseExists = course != null,
                    userExists = user != null
                });
            }

            if (user.Role.ToLower() != "student")
            {
                return BadRequest(new { message = "Only students can submit feedback." });
            }

            var existing = await _db.Feedbacks.FirstOrDefaultAsync(f =>
                f.CourseId == dto.CourseId && f.UserId == userId);

            if (existing != null)
            {
                return Conflict(new { message = "Feedback already submitted for this course." });
            }

            var feedback = new Feedback
            {
                CourseId = dto.CourseId,
                UserId = userId, 
                Rating = dto.Rating,
                Comments = string.IsNullOrWhiteSpace(dto.Comments) ? null : dto.Comments.Trim(),
                SubmittedAt = DateTime.UtcNow
            };

            try
            {
                _db.Feedbacks.Add(feedback);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new
                {
                    message = "Database error occurred while saving feedback.",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }

            var createdFeedback = await _db.Feedbacks
                .Include(f => f.User)
                .Include(f => f.Course)
                .Where(f => f.F_Id == feedback.F_Id)
                .Select(f => new
                {
                    f.F_Id,
                    f.CourseId,
                    f.UserId,
                    f.Rating,
                    f.Comments,
                    f.SubmittedAt,
                    Course = new
                    {
                        f.Course.CourseId,
                        f.Course.Course_Name
                    },
                    User = new
                    {
                        f.User.UserId,
                        f.User.Name
                    }
                })
                .FirstOrDefaultAsync();

            return CreatedAtAction(nameof(GetFeedbackById), new { id = feedback.F_Id }, createdFeedback);
        }

    }
}
