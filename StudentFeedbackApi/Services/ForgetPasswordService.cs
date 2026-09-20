
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StudentFeedbackApi.Data;
using StudentFeedbackApi.Models;


namespace StudentFeedbackApi.Services
{
    public class ForgetPasswordService : IForgetPasswordService
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;
        private readonly ILogger<ForgetPasswordService> _logger;

        private const int OtpLength = 6;
        private const int OtpExpiryMinutes = 10;
        private const int ResetTokenExpiryMinutes = 10;
        private const int MaxOtpAttempts = 5;
        private const int ResendCooldownSeconds = 30;

        public ForgetPasswordService(AppDbContext db, IEmailService emailService, ILogger<ForgetPasswordService> logger)
        {
            _db = db;
            _emailService = emailService;
            _logger = logger;
        }

        // ---------- STEP 1: send OTP ----------
        public async Task<ServiceResult> SendOtpAsync(string email)
        {
            email = email.Trim().ToLowerInvariant();

            // Recommended: check the email actually exists, but return the SAME
            // generic message either way so responses can't be used to enumerate
            // registered emails.
            var userExists = await _db.Users.AnyAsync(u => u.Email.ToLower() == email);
            if (!userExists)
                return ServiceResult.Ok("If that email is registered, a code has been sent.");

            // Rate limiting: block resend within cooldown window
            var lastOtp = await _db.PasswordResetOtps
                .Where(o => o.Email == email)
                .OrderByDescending(o => o.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (lastOtp != null &&
                (DateTime.UtcNow - lastOtp.CreatedAtUtc).TotalSeconds < ResendCooldownSeconds)
            {
                var waitSeconds = ResendCooldownSeconds - (int)(DateTime.UtcNow - lastOtp.CreatedAtUtc).TotalSeconds;
                return ServiceResult.Fail($"Please wait {waitSeconds}s before requesting another code.");
            }

            var otp = GenerateNumericOtp(OtpLength);
            var otpHash = Hash(otp);

            var entry = new PasswordResetOtp
            {
                Email = email,
                OtpHash = otpHash,
                OtpExpiryUtc = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                OtpUsed = false,
                FailedAttempts = 0,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.PasswordResetOtps.Add(entry);
            await _db.SaveChangesAsync();

            try
            {
                await _emailService.SendOtpEmailAsync(email, otp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", email);
                return ServiceResult.Fail("Could not send the email right now. Please try again shortly.");
            }

            return ServiceResult.Ok("Verification code sent to your email.");
        }

        // ---------- STEP 2: verify OTP, issue short-lived reset token ----------
        public async Task<ServiceResult> VerifyOtpAsync(string email, string otp)
        {
            email = email.Trim().ToLowerInvariant();

            var entry = await _db.PasswordResetOtps
                .Where(o => o.Email == email && !o.OtpUsed)
                .OrderByDescending(o => o.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (entry == null)
                return ServiceResult.Fail("No active request found. Please request a new code.");

            if (entry.OtpExpiryUtc < DateTime.UtcNow)
                return ServiceResult.Fail("This code has expired. Please request a new one.");

            if (entry.FailedAttempts >= MaxOtpAttempts)
                return ServiceResult.Fail("Too many incorrect attempts. Please request a new code.");

            if (!FixedTimeEquals(Hash(otp), entry.OtpHash))
            {
                entry.FailedAttempts++;
                await _db.SaveChangesAsync();
                return ServiceResult.Fail("Invalid code. Please try again.");
            }

            // OTP correct — issue an opaque, short-lived reset token.
            // Frontend should use this token in step 3 instead of resending the raw OTP.
            var resetToken = GenerateOpaqueToken();
            entry.ResetTokenHash = Hash(resetToken);
            entry.ResetTokenExpiryUtc = DateTime.UtcNow.AddMinutes(ResetTokenExpiryMinutes);
            entry.ResetTokenUsed = false;

            // OTP itself is now "spent" for verification purposes — only the
            // reset token can be used going forward.
            entry.OtpUsed = true;

            await _db.SaveChangesAsync();

            return ServiceResult.Ok("Code verified.", resetToken);
        }

        // ---------- STEP 3: reset password ----------
        public async Task<ServiceResult> ResetPasswordAsync(string email, string? resetToken, string? otp, string newPassword)
        {
            email = email.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return ServiceResult.Fail("Password must be at least 6 characters.");

            var entry = await _db.PasswordResetOtps
                .Where(o => o.Email == email)
                .OrderByDescending(o => o.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (entry == null)
                return ServiceResult.Fail("No active reset request found. Please start again.");

            var authorized = false;

            // Preferred path: verify via the reset token issued in step 2.
            if (!string.IsNullOrEmpty(resetToken)
                && entry.ResetTokenHash != null
                && !entry.ResetTokenUsed
                && entry.ResetTokenExpiryUtc.HasValue
                && entry.ResetTokenExpiryUtc.Value >= DateTime.UtcNow
                && FixedTimeEquals(Hash(resetToken), entry.ResetTokenHash))
            {
                authorized = true;
            }
            // Fallback path: frontend sends the raw OTP again instead of a token.
            else if (!string.IsNullOrEmpty(otp)
                     && entry.OtpUsed // was verified in step 2
                     && entry.ResetTokenExpiryUtc.HasValue
                     && entry.ResetTokenExpiryUtc.Value >= DateTime.UtcNow
                     && FixedTimeEquals(Hash(otp), entry.OtpHash))
            {
                authorized = true;
            }

            if (!authorized)
                return ServiceResult.Fail("This reset request is invalid or has expired. Please start again.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
                return ServiceResult.Fail("Account not found.");

            // Stored as plaintext to match Program.cs's current register/login
            // logic (u.Password == dto.Password).
            user.Password = newPassword;
            _db.Users.Update(user);

            entry.ResetTokenUsed = true; // invalidate — single use
            await _db.SaveChangesAsync();

            return ServiceResult.Ok("Password has been reset successfully.");
        }

        // ---------- helpers ----------

        private static string GenerateNumericOtp(int length)
        {
            Span<byte> bytes = stackalloc byte[4];
            RandomNumberGenerator.Fill(bytes);
            var value = BitConverter.ToUInt32(bytes) % (uint)Math.Pow(10, length);
            return value.ToString(new string('0', length));
        }

        private static string GenerateOpaqueToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static string Hash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            var bytesA = Encoding.UTF8.GetBytes(a);
            var bytesB = Encoding.UTF8.GetBytes(b);
            if (bytesA.Length != bytesB.Length) return false;
            return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
        }
    }
}
