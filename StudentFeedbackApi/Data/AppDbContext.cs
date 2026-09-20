using Microsoft.EntityFrameworkCore;
using StudentFeedbackApi.Models;

namespace StudentFeedbackApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.User)
                .WithMany(u => u.Feedbacks)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Course)
                .WithMany(c => c.Feedbacks)
                .HasForeignKey(f => f.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasswordResetOtp>(entity =>
       {
           entity.HasIndex(e => e.Email);
           entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
           entity.Property(e => e.OtpHash).IsRequired().HasMaxLength(128);
       });
        }
    }



}

