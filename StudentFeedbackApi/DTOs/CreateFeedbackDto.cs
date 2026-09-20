using System.ComponentModel.DataAnnotations;

namespace StudentFeedbackApi.DTOs
{
        public class CreateFeedbackDto
        {
            [Required(ErrorMessage = "CourseId is required.")]
            [Range(1, int.MaxValue, ErrorMessage = "CourseId must be a positive number.")]
            public int CourseId { get; set; }

            [Required(ErrorMessage = "Rating is required.")]
            [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
            public int Rating { get; set; }

            [MaxLength(1000, ErrorMessage = "Comments cannot exceed 1000 characters.")]
            public string? Comments { get; set; }
        
    }

}
