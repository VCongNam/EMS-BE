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
            try { 
                return Ok(await authService.RegisterAsync(request)); 
            }
            catch (Exception ex) { 
                return BadRequest(new { Message = ex.Message }); 
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
        {
            try
            {
                await authService.VerifyEmailAsync(request);
                return Ok(new { Message = "Xác thực thành công!" });
            }
            catch (Exception ex) { 
                return BadRequest(new { Message = ex.Message }); 
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try { 
                return Ok(await authService.LoginAsync(request)); 
            }
            catch (Exception ex) { 
                return BadRequest(new { Message = ex.Message }); 
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            try { 
                return Ok(await authService.ForgotPasswordAsync(request)); 
            }
            catch (Exception ex) { 
                return BadRequest(new { Message = ex.Message }); 
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            try { 
                return Ok(await authService.ResetPasswordAsync(request)); 
            }
            catch (Exception ex) { 
                return BadRequest(new { Message = ex.Message }); 
            }
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
            // .NET sẽ tự động check Validation của DTO ở đây
            try
            {
                await authService.ResendOtpAsync(request);
                return Ok(new { Message = "Mã OTP mới đã được gửi. Vui lòng kiểm tra hòm thư!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
