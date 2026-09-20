using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentFeedbackApi.Data;
using StudentFeedbackApi.DTOs;
using StudentFeedbackApi.Models;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using StudentFeedbackApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ================= JWT SETTINGS =================
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtConfig"));
var jwtConfig = builder.Configuration.GetSection("JwtConfig").Get<JwtSettings>()
                ?? throw new InvalidOperationException("JWT configuration is missing or invalid.");
var secretKey = jwtConfig.Key;

// ================= DATABASE =================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ================= FORGOT PASSWORD SERVICES =================
builder.Services.AddScoped<IForgetPasswordService, ForgetPasswordService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// ================= JSON HANDLING =================
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
});

// ================= AUTHENTICATION =================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig.Issuer,
        ValidAudience = jwtConfig.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// ================= CORS =================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ================= MIDDLEWARE =================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// =================== ADMIN SEEDER ===================
await AdminSeeder.SeedAdminAsync(app.Services);

// =================== AUTH ROUTES ===================
app.MapPost("/api/register", async (RegisterDto dto, AppDbContext db) =>
{
    if (await db.Users.AnyAsync(u => u.Email == dto.Email))
        return Results.BadRequest(new { message = "Email already registered." });

    var user = new User
    {
        Name = dto.Name,
        Email = dto.Email,
        Password = dto.Password,
        Role = dto.Role
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new
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
});

app.MapPost("/api/login", async (LoginDto dto, AppDbContext db) =>
{
    var user = await db.Users
        .FirstOrDefaultAsync(u => u.Email == dto.Email);

    if (user == null)
        return Results.BadRequest(new { message = "Invalid email or password." });

    bool passwordValid;

    if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(
                dto.Password,
                user.Password
            );
        }
        catch
        {
            passwordValid = false;
        }
    }
    else
    {
        passwordValid = user.Password == dto.Password;
    }

    if (!passwordValid)
        return Results.BadRequest(new { message = "Invalid email or password." });

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Name),
        new Claim(ClaimTypes.Role, user.Role)
    };

    var token = new JwtSecurityToken(
        issuer: jwtConfig.Issuer,
        audience: jwtConfig.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(jwtConfig.DurationInMinutes),
        signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            SecurityAlgorithms.HmacSha256)
    );

    return Results.Ok(new
    {
        token = new JwtSecurityTokenHandler().WriteToken(token),
        role = user.Role
    });
});

// =================== COURSE ROUTES ===================

app.MapPost("/api/courses", [Authorize(Roles = "Admin,Faculty")] async (HttpContext http, Course course, AppDbContext db) =>
{
    var creatorName = http.User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
    course.CreatedBy = creatorName;

    db.Courses.Add(course);
    await db.SaveChangesAsync();

    return Results.Created($"/api/courses/{course.CourseId}", course);
});

app.MapDelete("/api/courses/{id:int}", [Authorize(Roles = "Admin,Faculty")] async (int id, AppDbContext db) =>
{
    var course = await db.Courses
        .Include(c => c.Feedbacks)
        .FirstOrDefaultAsync(c => c.CourseId == id);

    if (course == null)
        return Results.NotFound(new { message = "Course not found." });

    db.Feedbacks.RemoveRange(course.Feedbacks);
    db.Courses.Remove(course);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Course and its feedbacks deleted." });
});

app.Run();
