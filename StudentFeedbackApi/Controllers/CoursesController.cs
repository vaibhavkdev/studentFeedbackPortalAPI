using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedbackApi.Data;
using StudentFeedbackApi.Models;
using System.Security.Claims;

namespace StudentFeedbackApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CoursesController(AppDbContext db)
        {
            _db = db;
        }

        // GET: api/Courses
        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            try
            {
                var courses = await _db.Courses.ToListAsync();
                return Ok(courses);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetCourses] Error: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching courses.");
            }
        }

        // POST: api/Courses
        [Authorize(Roles = "Admin,Faculty")]
        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromBody] Course course)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                bool exists = await _db.Courses.AnyAsync(c => c.Course_Name == course.Course_Name);
                if (exists)
                    return Conflict("A course with the same name already exists.");

                // Get creator name from JWT token
                course.CreatedBy = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

                _db.Courses.Add(course);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCourses), new { id = course.CourseId }, course);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateCourse] Error: {ex.Message}");
                return StatusCode(500, "An error occurred while creating the course.");
            }
        }


        // PUT: api/Courses/{id}
        [Authorize(Roles = "Admin,Faculty")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] Course updatedCourse)
        {
            if (id != updatedCourse.CourseId)
                return BadRequest("Course ID mismatch.");

            try
            {
                var course = await _db.Courses.FindAsync(id);
                if (course == null)
                    return NotFound("Course not found.");

                bool duplicate = await _db.Courses
                    .AnyAsync(c => c.Course_Name == updatedCourse.Course_Name && c.CourseId != id);
                if (duplicate)
                    return Conflict("Another course with the same name already exists.");

                course.Course_Name = updatedCourse.Course_Name;
                course.Description = updatedCourse.Description;

                await _db.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateCourse] Error: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the course.");
            }
        }

        // DELETE: api/Courses/{id}
        [Authorize(Roles = "Admin,Faculty")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var course = await _db.Courses.FindAsync(id);
                if (course == null)
                    return NotFound("Course not found.");

                _db.Courses.Remove(course);
                await _db.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DeleteCourse] Error: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the course.");
            }
        }
    }
}
