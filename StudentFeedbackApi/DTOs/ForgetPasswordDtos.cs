namespace StudentFeedbackApi.DTOs
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyOtpRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;

        // Returned by /verify-otp. Optional so the OTP-only flow keeps working
        // — the service accepts either ResetToken OR falls back to Otp.
        public string? ResetToken { get; set; }
        public string? Otp { get; set; }

        public string NewPassword { get; set; } = string.Empty;
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }

        public static ApiResponse Ok(string message, object? data = null) =>
            new ApiResponse { Success = true, Message = message, Data = data };

        public static ApiResponse Fail(string message) =>
            new ApiResponse { Success = false, Message = message };
    }
}
