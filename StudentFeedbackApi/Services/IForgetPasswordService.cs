using System.Threading.Tasks;

namespace StudentFeedbackApi.Services
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ResetToken { get; set; }

        public static ServiceResult Ok(string message, string? resetToken = null) =>
            new ServiceResult { Success = true, Message = message, ResetToken = resetToken };

        public static ServiceResult Fail(string message) =>
            new ServiceResult { Success = false, Message = message };
    }

    public interface IForgetPasswordService
    {
        Task<ServiceResult> SendOtpAsync(string email);
        Task<ServiceResult> VerifyOtpAsync(string email, string otp);
        Task<ServiceResult> ResetPasswordAsync(string email, string? resetToken, string? otp, string newPassword);
    }
}
