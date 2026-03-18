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

        // [GET] /api/Account/profile
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

        // [PUT] /api/Account/profile
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

        // [POST] /api/Account/change-password (Dành cho user đang đăng nhập)
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            try
            {
                var accountId = GetAccountIdFromToken();
                await accountService.ChangePassewordAsync(accountId, request);
                return Ok(new { Message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }


    }
}
