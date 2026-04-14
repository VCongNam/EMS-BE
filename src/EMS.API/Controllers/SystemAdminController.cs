using EMS.Application.Features.Feedbacks.Dtos;
using EMS.Application.Features.Feedbacks.Services;
using EMS.Application.Features.SystemAdmin.Dtos;
using EMS.Application.Features.SystemAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class SystemAdminController : ControllerBase
    {
        private readonly ISystemAdminService _adminService;
        private readonly IFeedbackService _feedbackService;

        public SystemAdminController(ISystemAdminService adminService, IFeedbackService feedbackService)
        {
            _adminService = adminService;
            _feedbackService = feedbackService;
        }

        // --- QUẢN LÝ HỆ THỐNG & DASHBOARD ---

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetSystemDashboard([FromQuery] DashboardFilterDto filter)
        {
            try
            {
                var result = await _adminService.GetSystemDashboardAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("teachers")]
        public async Task<IActionResult> GetTeachersList([FromQuery] string? searchTerm, [FromQuery] string? statusFilter)
        {
            try
            {
                var result = await _adminService.GetTeachersGridAsync(searchTerm, statusFilter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("teachers/{id}")]
        public async Task<IActionResult> GetTeacherDetail(Guid id)
        {
            try
            {
                var result = await _adminService.GetTeacherDetailAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }

        // --- QUẢN LÝ FEEDBACK ---

        [HttpGet("feedbacks")]
        public async Task<IActionResult> GetFeedbackList([FromQuery] string? type, [FromQuery] string? status)
        {
            try
            {
                var result = await _feedbackService.GetAdminListAsync(type, status);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("feedbacks/{id}/process")]
        public async Task<IActionResult> ProcessFeedback(Guid id, [FromBody] ProcessFeedbackDto dto)
        {
            try
            {
                await _feedbackService.ProcessFeedbackAsync(id, dto);
                return Ok(new { Message = "Đã cập nhật trạng thái và gửi thông báo cho người dùng." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}