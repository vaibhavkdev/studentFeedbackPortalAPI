using Microsoft.AspNetCore.Mvc;
using StudentFeedbackApi.DTOs;
using StudentFeedbackApi.Services;

namespace StudentFeedbackApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForgetPasswordController : ControllerBase
    {
        private readonly IForgetPasswordService _forgetPasswordService;

        public ForgetPasswordController(IForgetPasswordService forgetPasswordService)
        {
            _forgetPasswordService = forgetPasswordService;
        }

        // POST: api/ForgetPassword/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(ApiResponse.Fail("Email is required."));

            var result = await _forgetPasswordService.SendOtpAsync(request.Email);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Message));

            return Ok(ApiResponse.Ok(result.Message));
        }

        // POST: api/ForgetPassword/verify-otp
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
                return BadRequest(ApiResponse.Fail("Email and code are required."));

            var result = await _forgetPasswordService.VerifyOtpAsync(request.Email, request.Otp);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Message));

            return Ok(ApiResponse.Ok(result.Message, new { resetToken = result.ResetToken }));
        }

        // POST: api/ForgetPassword/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(ApiResponse.Fail("Email and new password are required."));

            var result = await _forgetPasswordService.ResetPasswordAsync(
                request.Email, request.ResetToken, request.Otp, request.NewPassword);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Message));

            return Ok(ApiResponse.Ok(result.Message));
        }
    }
}