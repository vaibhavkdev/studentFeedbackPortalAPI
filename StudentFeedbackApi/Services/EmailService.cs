using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace StudentFeedbackApi.Services
{
    // Uses MailKit (NuGet: MailKit). Works with Gmail SMTP, SendGrid SMTP relay,
    // Office365, or any standard SMTP provider — just change appsettings.
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            var smtpSection = _config.GetSection("SmtpSettings");
            var host = smtpSection["Host"];
            var port = int.Parse(smtpSection["Port"] ?? "587");
            var username = smtpSection["Username"];
            var password = smtpSection["Password"];
            var fromEmail = smtpSection["FromEmail"];
            var fromName = smtpSection["FromName"] ?? "Support";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Your password reset code";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <div style='font-family:Arial,sans-serif;max-width:480px;margin:auto'>
                        <h2 style='color:#16213E'>Password reset code</h2>
                        <p>Use the code below to reset your password. It expires in 10 minutes.</p>
                        <div style='font-size:28px;font-weight:bold;letter-spacing:6px;
                                    background:#F5F3EE;padding:16px;text-align:center;
                                    border-radius:8px;color:#16213E'>{otp}</div>
                        <p style='color:#6B7280;font-size:13px;margin-top:16px'>
                            If you didn't request this, you can safely ignore this email.
                        </p>
                    </div>"
            };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
