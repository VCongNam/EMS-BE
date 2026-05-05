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
        private readonly IStudentAccountService _studentAccountService;

        public AccountController(IAccountService accountService, ICurrentUserService currentUserService, IStudentAccountService studentAccountService)
        {
            this.accountService = accountService;
            this.currentUserService = currentUserService;
            _studentAccountService = studentAccountService;
        }


        // [GET] /api/Account/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
          
                var accountId = currentUserService.UserId;

                if (accountId == Guid.Empty)
                    return Unauthorized(new { Message = "Không tìm thấy User." });

                var profile = await accountService.GetProfileAsync(accountId);
                return Ok(profile);
           
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

        [HttpPost("student/create")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto request)
        {
            try
            {
                var result = await _studentAccountService.CreateStudentAsync(request);

                return Ok(new
                {
                    message = "Create student account successfully!",
                    studentId = result.StudentId,
                    password = result.InitialPassword,
                    isNewAccount = result.IsNewAccount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("student/import-excel")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            try
            {
                var result = await _studentAccountService.ImportStudentsFromExcelAsync(file);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        [HttpPut("student/{studentId}/profile")]
        [Authorize(Roles = "Student")] 
        public async Task<IActionResult> UpdateStudentProfile(Guid studentId, [FromBody] UpdateStudentProfileRequest request)
        {
            try
            {
                var accountId = currentUserService.UserId; // Lấy AccountId từ Token

                // Truyền cả accountId (để xác thực) và studentId (để định vị hồ sơ)
                var updatedProfile = await accountService.UpdateStudentProfileAsync(accountId, studentId, request);
                return Ok(updatedProfile);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        //Reset student password
        [HttpPut("student/reset-password")]
        public async Task<IActionResult> ResetStudentPassword([FromBody] ResetStudentPasswordDto request)
        {
            try
            {
                await _studentAccountService.ResetStudentPasswordAsync(request.StudentId, request.NewPassword);

                return Ok(new { Message = "Đã cấp lại mật khẩu thành công cho học sinh!" });
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPut("avatar")]
        public async Task<IActionResult> UpdateAvatar([FromForm] UploadAvatarDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var newAvatarUrl = await accountService.UpdateAvatarAsync(request);

                return Ok(new
                {
                    Message = "Cập nhật ảnh đại diện thành công!",
                    AvatarUrl = newAvatarUrl
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
