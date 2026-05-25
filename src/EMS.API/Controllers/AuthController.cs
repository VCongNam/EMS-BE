using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Auth.DTOs;
using EMS.Application.Features.Auth.Services;
using EMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;
        public AuthController(IAuthService authService)
        { 
            this.authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        { 
            return Ok(await authService.RegisterAsync(request)); 
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
        {
                await authService.VerifyEmailAsync(request);
                return Ok(new { Message = "Xác thực thành công!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
                return Ok(await authService.LoginAsync(request)); 
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
                return Ok(await authService.ForgotPasswordAsync(request)); 
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
                return Ok(await authService.ResetPasswordAsync(request)); 
        }

        [Authorize(Roles = "Student")]
        [HttpPost("select-profile")]
        public async Task<IActionResult> SelectProfile([FromBody] SelectProfileRequest request)
        {
            var tokenType = User.FindFirst("TokenType")?.Value;
            if (tokenType != "Temp") return BadRequest(new { Message = "Token không hợp lệ." });

            return Ok(await authService.SelectProfileAsync(request.StudentId));
        }

        [HttpPost("verify-onboarding")]
        public async Task<IActionResult> VerifyOnboarding([FromBody] OnboardingRequest request)
        { 
            await authService.VerifyOnboardingAsync(request);
            return Ok(new { Message = "Kích hoạt tài khoản thành công!" });
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
        {
                await authService.ResendOtpAsync(request);
                return Ok(new { Message = "Mã OTP mới đã được gửi. Vui lòng kiểm tra hòm thư!" });
        }
    }
}
