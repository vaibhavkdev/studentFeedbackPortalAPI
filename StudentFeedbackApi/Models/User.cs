using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StudentFeedbackApi.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "student"; // "student" or "admin" or "faculty"

        [JsonIgnore]
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
