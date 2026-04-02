using EMS.Application.Features.SystemAdmin.Dtos;
using EMS.Application.Features.SystemAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")] // Bức tường lửa: Chỉ Admin mới được truy cập
    public class SystemAdminsController : ControllerBase
    {
        private readonly ISystemAdminService adminService;

        public SystemAdminsController(ISystemAdminService adminService)
        {
            this.adminService = adminService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var dashboard = await adminService.GetSystemDashboardAsync();
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("accounts")]
        public async Task<IActionResult> GetAllAccounts([FromQuery] string? role, [FromQuery] string? status)
        {
            try
            {
                var accounts = await adminService.GetAllAccountsAsync(role, status);
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("accounts/{id}")]
        public async Task<IActionResult> GetAccountDetail(Guid id)
        {
            try
            {
                var account = await adminService.GetAccountDetailAsync(id);
                return Ok(account);
            }
            catch (Exception ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [HttpPatch("accounts/{id}/status")]
        public async Task<IActionResult> ChangeAccountStatus(Guid id, [FromBody] ChangeAccountStatusDto request)
        {
            try
            {
                await adminService.ChangeAccountStatusAsync(id, request);
                return Ok(new { Message = $"Đã chuyển trạng thái tài khoản thành: {request.NewStatus} và gửi email thông báo." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetSystemLogs([FromQuery] int limit = 50)
        {
            try
            {
                var logs = await adminService.GetSuspiciousActivitiesAsync(limit);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
