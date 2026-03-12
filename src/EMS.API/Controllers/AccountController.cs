using EMS.Application.Features.Accounts.DTOs;
using EMS.Application.Features.Accounts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]// yêu cầu phải có authorize
    public class AccountController : ControllerBase
    {
        private readonly AccountService accountService;

        public AccountController(AccountService accountService)
        {
            this.accountService = accountService;
        }


        private Guid GetAccountIdFromToken()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdString))
                throw new Exception("Không tìm thấy thông tin User trong Token.");

            return Guid.Parse(userIdString);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var accountId = GetAccountIdFromToken();
                var profile = await accountService.GetProfileAsync(accountId);
                return Ok(profile);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
        {
            try
            {
                var accountId = GetAccountIdFromToken();
                var updatedProfile = await accountService.UpdateProfileAsync(accountId, request);
                return Ok(updatedProfile);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            try
            {
                var accountId = GetAccountIdFromToken();
                await accountService.ResetPasswordAsync(accountId, request);
                return Ok(new { Message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }
    }
}
