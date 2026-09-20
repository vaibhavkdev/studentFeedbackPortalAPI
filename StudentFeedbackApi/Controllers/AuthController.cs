using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StudentFeedbackApi.Data;
using StudentFeedbackApi.DTOs;
using StudentFeedbackApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StudentFeedbackApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (existingUser != null)
                return BadRequest(new { message = "Email is already registered." });

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Registration successful",
                user = new
                {
                    user.UserId,
                    user.Name,
                    user.Email,
                    user.Role
                }
            });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Look up by email only now — this same lookup finds the admin row
            // (seeded/hashed by AdminSeeder.cs) as well as normal student/faculty rows.
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
                return BadRequest(new { message = "Invalid email or password." });

            bool passwordValid;
            try
            {
                // Verifies bcrypt-hashed passwords — this is how the admin row is stored.
                passwordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password);
            }
            catch
            {
                // Falls back to plain-text comparison for older student rows
                // registered before hashing was added.
                passwordValid = user.Password == loginDto.Password;
            }

            if (!passwordValid)
                return BadRequest(new { message = "Invalid email or password." });

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                role = user.Role
            });
        }

        private string GenerateJwtToken(User user)
        {
            string? keyString = _config["JwtSettings:Key"];

            if (string.IsNullOrEmpty(keyString))
                throw new InvalidOperationException("JWT Key is not configured in appsettings.json");

            var key = Encoding.ASCII.GetBytes(keyString);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                     new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["JwtSettings:DurationInMinutes"] ?? "60")),
                Issuer = _config["JwtSettings:Issuer"],
                Audience = _config["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
