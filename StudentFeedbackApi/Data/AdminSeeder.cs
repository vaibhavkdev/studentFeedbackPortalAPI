using Microsoft.EntityFrameworkCore;
using StudentFeedbackApi.Models;

namespace StudentFeedbackApi.Data
{
    public static class AdminSeeder
    {
        /// <summary>
        /// Reads admin email/password from configuration (AdminSettings:Email, AdminSettings:Password),
        /// hashes the password, and creates or updates the Admin user in the database.
        /// Call this once at startup, after the app is built and before app.Run().
        /// </summary>
        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetService<ILogger<object>>();

            string? adminEmail = config["AdminSettings:Email"];
            string? adminPassword = config["AdminSettings:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                logger?.LogWarning("AdminSettings:Email or AdminSettings:Password not configured — skipping admin seed.");
                return;
            }

            var existingAdmin = await db.Users
                .FirstOrDefaultAsync(u => u.Email == adminEmail);

            if (existingAdmin == null)
            {
                db.Users.Add(new User
                {
                    Name = "Admin",
                    Email = adminEmail,
                    Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Role = "Admin"
                });

                await db.SaveChangesAsync();
                logger?.LogInformation("Admin user created for {Email}.", adminEmail);
                return;
            }

            bool passwordMatches;
            try
            {
                passwordMatches = BCrypt.Net.BCrypt.Verify(adminPassword, existingAdmin.Password);
            }
            catch
            {
                // Existing value wasn't a valid bcrypt hash (e.g. legacy plain-text row)
                passwordMatches = false;
            }

            existingAdmin.Role = "Admin";

            if (!passwordMatches)
            {
                existingAdmin.Password = BCrypt.Net.BCrypt.HashPassword(adminPassword);
            }

            await db.SaveChangesAsync();

            logger?.LogInformation(
                "Admin user verified/updated for {Email}.",
                adminEmail
            );
        }
    }
}
