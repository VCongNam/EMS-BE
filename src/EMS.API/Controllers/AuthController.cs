using EMS.Application.Features.Accounts.Services;
using Microsoft.AspNetCore.Mvc;
using EMS.Application.Features.Accounts.DTOs;
namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AccountService accountService;
        public AuthController(AccountService accountService)
        { 
            this.accountService = accountService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try { 
                return Ok(await accountService.RegisterAsync(request)); 
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
                await accountService.VerifyEmailAsync(request);
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
                return Ok(await accountService.LoginAsync(request)); 
            }
            catch (Exception ex) { 
                return BadRequest(new { Message = ex.Message }); 
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            try { 
                return Ok(await accountService.ForgotPasswordAsync(request)); 
            }
            catch (Exception ex) { 
                return BadRequest(new { Message = ex.Message }); 
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            try { 
                return Ok(await accountService.ResetPasswordAsync(request)); 
            }
            catch (Exception ex) { 
                return BadRequest(new { Message = ex.Message }); 
            }
        }
    }
}
