namespace StudentFeedbackApi.Models
{
    // One row per OTP request. Never store the raw OTP or raw token — only hashes.
    public class PasswordResetOtp
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        // SHA-256 hash of the 6-digit OTP (never store plaintext)
        public string OtpHash { get; set; } = string.Empty;

        public DateTime OtpExpiryUtc { get; set; }

        public bool OtpUsed { get; set; }

        public int FailedAttempts { get; set; }

        // Populated only after OTP is verified successfully.
        // Hash of a short-lived opaque token the frontend uses for step 3,
        // so the raw OTP never has to travel again.
        public string? ResetTokenHash { get; set; }

        public DateTime? ResetTokenExpiryUtc { get; set; }

        public bool ResetTokenUsed { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
