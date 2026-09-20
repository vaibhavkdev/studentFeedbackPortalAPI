using System.Threading.Tasks;

namespace StudentFeedbackApi.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string otp);
    }
}
