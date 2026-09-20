using System.ComponentModel.DataAnnotations;

namespace StudentFeedbackApi.DTOs
{
    public class CreateCourseDto
    {
        [Required(ErrorMessage = "Course name is required.")]
        [MaxLength(100, ErrorMessage = "Course name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }
    }
}
