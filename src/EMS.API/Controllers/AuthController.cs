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

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try
            {
                var response = await accountService.LoginAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try
            {
                var response = await accountService.RegisterAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { Message = "Đăng xuất thành công. Vui lòng xóa Token ở LocalStorage của Frontend." });
        }

        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            // TODO: Ghép logic gửi Email thật vào đây
            return Ok(new
            {
                Message = $"Tính năng đang hoàn thiện. Một email khôi phục mật khẩu sẽ được gửi đến {request.Email} khi tích hợp SMTP."
            });
        }
    }
}
