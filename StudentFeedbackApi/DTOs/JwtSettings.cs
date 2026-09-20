using System.ComponentModel.DataAnnotations;

namespace StudentFeedbackApi.DTOs
{
    public class JwtSettings
    {
        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        [Required]
        public string Key { get; set; } = string.Empty;

        [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes")]
        public int DurationInMinutes { get; set; }
    }
}
