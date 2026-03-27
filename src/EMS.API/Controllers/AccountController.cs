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
        private readonly IImageService imageService;

        public AccountController(IAccountService accountService, ICurrentUserService currentUserService, IImageService imageService)
        {
            this.accountService = accountService;
            this.currentUserService = currentUserService;
            this.imageService = imageService;
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
                await accountService.ChangePasswordAsync(accountId, request);

                return Ok(new { Message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // 1. API UPDATE TEACHER
        [HttpPut("teacher/profile")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> UpdateTeacherProfile(UpdateTeacherProfileRequest request)
        {
            try
            {
                var accountId = currentUserService.UserId;
                var updatedProfile = await accountService.UpdateTeacherProfileAsync(accountId, request);
                return Ok(updatedProfile);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // 2. API UPDATE TA
        [HttpPut("ta/profile")]
        [Authorize(Roles = "TA")]
        public async Task<IActionResult> UpdateTAProfile(UpdateTAProfileRequest request)
        {
            try
            {
                var accountId = currentUserService.UserId;
                var updatedProfile = await accountService.UpdateTAProfileAsync(accountId, request);
                return Ok(updatedProfile);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // 3. API UPDATE STUDENT
        [HttpPut("student/profile")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UpdateStudentProfile(UpdateStudentProfileRequest request)
        {
            try
            {
                var accountId = currentUserService.UserId;
                var updatedProfile = await accountService.UpdateStudentProfileAsync(accountId, request);
                return Ok(updatedProfile);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }


        // [POST] /api/Account/avatar
        [HttpPost("avatar")]
        public async Task<IActionResult> UpdateAvatar(IFormFile file)
        {
            try
            {
                var accountId = currentUserService.UserId;
                if (file == null || file.Length == 0) return BadRequest("Vui lòng chọn ảnh.");

                // BƯỚC 1: Đẩy ảnh mới lên Cloudinary
                var newImageUrl = await imageService.UploadAvatarAsync(file);

                // BƯỚC 2: Cập nhật DB và lấy Link cũ về
                var (newUrl, oldUrl) = await accountService.UpdateAvatarUrlAsync(accountId, newImageUrl);

                // BƯỚC 3: Xóa ảnh cũ trên Cloud (Chạy ngầm không bắt User chờ)
                if (!string.IsNullOrEmpty(oldUrl) && oldUrl.Contains("cloudinary.com"))
                {
                    // Sử dụng Task.Run để xóa ảnh cũ mà không làm chậm API trả về
                    _ = Task.Run(() => imageService.DeleteImageAsync(oldUrl));
                }

                return Ok(new { Message = "Cập nhật thành công", AvatarUrl = newUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

    }
}
