using System.ComponentModel.DataAnnotations;

namespace StudentFeedbackApi.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        [RegularExpression("^(Student|Faculty|Admin)$", ErrorMessage = "Role must be either Student, Faculty, or Admin")]
        public string Role { get; set; } = "Student"; 
    }
}
