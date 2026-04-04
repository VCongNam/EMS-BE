using EMS.Application.Features.Students.DTOs;
using EMS.Application.Features.Students.Services;
using EMS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;

namespace EMS.API.Controllers.Students
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentAssignmentController : ControllerBase
    {
        private readonly IStudentAssignmentService _studentAssignmentService;

        public StudentAssignmentController(IStudentAssignmentService studentAssignmentService)
        {
            _studentAssignmentService = studentAssignmentService;
        }

        [HttpGet("Assignments")]
        public async Task<IActionResult> GetAssignments(Guid classId, [FromQuery] AssignmentFilter filter)
        {
            try
            {
                var result = await _studentAssignmentService.GetClassAssignmentsAsync(classId, filter);
                return Ok(new
                {
                    Message = "Lấy danh sách bài tập thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("{assignmentId}/detail")]
        public async Task<IActionResult> GetAssignmentDetail(Guid assignmentId)
        {
            try
            {
                var result = await _studentAssignmentService.GetClassAssignmentsDetailAsync(assignmentId);
                return Ok(new
                {
                    Message = "Lấy chi tiết bài tập thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{assignmentId}/submit")]
        public async Task<IActionResult> SubmitAssignment(Guid assignmentId, [FromForm] SubmitAssignmentRequest request)
        {
            try
            {
                if (request.Files == null || !request.Files.Any())
                {
                    return BadRequest(new { Message = "Vui lòng đính kèm ít nhất 1 file." });
                }

                await _studentAssignmentService.SubmitAssignmentAsync(assignmentId, request);

                return Ok(new
                {
                    Message = "Nộp bài thành công!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("{assignmentId}/unsubmit")]
        public async Task<IActionResult> UnsubmitAssignment(Guid assignmentId)
        {
            try
            {
                await _studentAssignmentService.UnsubmitAssignmentAsync(assignmentId);

                return Ok(new
                {
                    Message = "Đã hủy nộp bài!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
