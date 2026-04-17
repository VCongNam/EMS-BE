using EMS.Application.Features.Assignments.DTOs;
using EMS.Application.Features.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssignmentController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;
        private readonly IStudentAssignmentService _studentAssignmentService;

        public AssignmentController(IAssignmentService assignmentService, IStudentAssignmentService studentAssignmentService)
        {
            _assignmentService = assignmentService;
            _studentAssignmentService = studentAssignmentService;
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

   

        [HttpPut("submissions/{submissionId}/grade")]
        public async Task<IActionResult> GradeSubmission(Guid submissionId, [FromBody] GradeSubmissionDto request)
        {
            try
            {
                await _assignmentService.GradeSubmissionAsync(submissionId, request);
                return Ok(new { Message = "Submission graded successfully" });
            }
            catch (Exception ex) { return BadRequest(new { Error = ex.Message }); }
        }

        [HttpPost("submissions/{submissionId}/feedback")]
        public async Task<IActionResult> GiveFeedback(Guid submissionId, [FromBody] FeedbackSubmissionDto request)
        {
            try
            {
                await _assignmentService.GiveFeedbackAsync(submissionId, request);
                return Ok(new { Message = "Feedback given successfully" });
            }
            catch (Exception ex) { return BadRequest(new { Error = ex.Message }); }
        }

       [HttpPost("assignments/{assignmentId}/offline-grade")]
        public async Task<IActionResult> OfflineGrade(Guid assignmentId, [FromBody] OfflineGradeDto request)
        {
            try
            {
                var submissionId = await _assignmentService.OfflineGradeAsync(assignmentId, request);
                return Ok(new { SubmissionId = submissionId, Message = "Offline grade saved successfully" });
            }
            catch (Exception ex) { return BadRequest(new { Error = ex.Message }); }
        }

        [HttpGet("{assignmentId}/submissions")]
        public async Task<IActionResult> GetAssignmentSubmissions(Guid assignmentId)
        {
            try
            {
                var result = await _assignmentService.GetSubmissionsForAssignmentAsync(assignmentId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message); 
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message }); 
            }
        }

        [HttpGet("{assignmentId}/submissions/{studentId}")]
        public async Task<IActionResult> GetStudentSubmissionDetail(Guid assignmentId, Guid studentId)
        {
            try
            {
                var result = await _assignmentService.GetStudentSubmissionDetailAsync(assignmentId, studentId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("{assignmentId}/submissions/{studentId}/has-submitted")]
        public async Task<IActionResult> CheckStudentSubmitted(Guid assignmentId, Guid studentId)
        {
            try
            {
                var hasSubmitted = await _assignmentService.HasStudentSubmittedAsync(assignmentId, studentId);
                return Ok(new { AssignmentId = assignmentId, StudentId = studentId, HasSubmitted = hasSubmitted });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Student feature

        [HttpGet("student/assignments")]
        [Authorize(Roles = "Student")]
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

        [HttpGet("student/{assignmentId}/detail")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentAssignmentDetail(Guid assignmentId)
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

        [HttpPost("student/{assignmentId}/submit")]
        [Authorize(Roles = "Student")]
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

        [HttpDelete("student/{assignmentId}/unsubmit")]
        [Authorize(Roles = "Student")]
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
