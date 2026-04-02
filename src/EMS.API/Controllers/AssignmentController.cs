using EMS.Application.Features.Assignments.DTOs;
using EMS.Application.Features.Assignments.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssignmentController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;

        public AssignmentController(IAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAssignment([FromForm] CreateAssignmentDto request)
        {
            try
            {
                var id = await _assignmentService.CreateAssignmentAsync(request);
                return Ok(new { AssignmentId = id, Message = "Assignment created successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAssignment(Guid id, [FromForm] UpdateAssignmentDto request)
        {
            try
            {
                await _assignmentService.UpdateAssignmentAsync(id, request);
                return Ok(new { Message = "Assignment updated successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAssignment(Guid id)
        {
            try
            {
                await _assignmentService.DeleteAssignmentAsync(id);
                return Ok(new { Message = "Assignment deleted successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Xem chi tiết assignment (kèm attachments)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAssignmentDetail(Guid id)
        {
            try
            {
                var result = await _assignmentService.GetAssignmentDetailAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }

        // Xem toàn bộ assignment của 1 lớp
        [HttpGet("class/{classId}")]
        public async Task<IActionResult> GetAssignmentsByClass(Guid classId)
        {
            var assignments = await _assignmentService.GetAssignmentsByClassIdAsync(classId);
            return Ok(assignments);
        }

        // Xem danh sách submissions của 1 assignment
        [HttpGet("{id}/submissions")]
        public async Task<IActionResult> GetAssignmentSubmissions(Guid id)
        {
            try
            {
                var result = await _assignmentService.GetAssignmentSubmissionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }
    }
}
