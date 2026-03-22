using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Accounts.DTOs;
using EMS.Application.Features.Accounts.Services;
using EMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Security.Claims;


namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]// yêu cầu phải có authorize
    public class AccountController : ControllerBase
    {
        private readonly IAccountService accountService;
        private readonly ICurrentUserService currentUserService;

        public AccountController(IAccountService accountService, ICurrentUserService currentUserService)
        {
            this.accountService = accountService;
            this.currentUserService = currentUserService;
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
                var accountId = currentUserService.UserId;

                if (accountId == Guid.Empty)
                    return Unauthorized(new { Message = "Không tìm thấy User." });

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
                var accountId = currentUserService.UserId;
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
                // Đã thay đổi: Sử dụng currentUserService thay vì GetAccountIdFromToken
                var accountId = currentUserService.UserId;

                if (accountId == Guid.Empty)
                    return Unauthorized(new { Message = "Không tìm thấy User trong Token." });

                // Lưu ý: Tên hàm trong Service đang bị typo 'Passeword', hãy khớp với Service của bạn
                await accountService.ChangePassewordAsync(accountId, request);

                return Ok(new { Message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


    }
}
