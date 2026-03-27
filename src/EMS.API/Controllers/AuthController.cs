using EMS.Application.Features.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using EMS.Application.Features.Auth.DTOs;
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
    }
}
