using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Classes.Services;
using EMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TeachingAssistantsController : ControllerBase
    {
        private readonly IClassTAService _classTAService;
        public TeachingAssistantsController(IClassTAService classTAService)
        {
            _classTAService = classTAService;
        }

        [HttpGet("myTas")]
        public async Task<IActionResult> GetMyTeachingAssistants()
        {
            try
            {
                var result = await _classTAService.GetTAsByTeacherIdAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("getByMail")]
        public async Task<IActionResult> SearchTAByEmail([FromQuery] string email)
        {
            try
            {
                var result = await _classTAService.FindTAByEmailAsync(email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPatch("{taskId}/status")]
        [Authorize(Roles = "TA")]
        public async Task<IActionResult> UpdateTaskStatus(Guid taskId, [FromBody] UpdateTaskStatusDto status)
        {
            try
            {
                await _classTAService.UpdateTaskStatusAsync(taskId, status);
                return Ok(new { message = "Cập nhật trạng thái thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{taskId}/review")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ReviewTask(Guid taskId, [FromBody] ReviewTaskDto request)
        {
            try
            {
                await _classTAService.ReviewTaskAsync(taskId, request.IsApproved, request.Feedback);
                string msg = request.IsApproved ? "Đã duyệt nhiệm vụ." : "Đã từ chối nhiệm vụ.";
                return Ok(new { message = msg });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
