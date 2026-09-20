using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StudentFeedbackApi.Models
{
    public class Course
    {
        [Key]
        public int CourseId { get; set; }

        [Required]
        public string Course_Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        // Prevent serialization cycle
        [JsonIgnore]
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
