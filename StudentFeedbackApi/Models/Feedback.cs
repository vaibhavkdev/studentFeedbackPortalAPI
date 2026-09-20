using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StudentFeedbackApi.Models
{
    public class Feedback
    {
        [Key]
        public int F_Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        public string? Comments { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        // [JsonIgnore] 
        public Course Course { get; set; } = null!;

        // [JsonIgnore] 
        public User User { get; set; } = null!;
    }
}
