using EMS.Application.Features.Students.DTOs;
using EMS.Application.Features.Students.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet]
        public async Task<IActionResult> GetAssignments(Guid classId, [FromQuery] AssignmentFilter filter)
        {
            var result = await _studentAssignmentService.GetClassAssignmentsAsync(classId, filter);
            return Ok(new
            {
                Message = "Lấy danh sách bài tập thành công",
                Data = result
            });
        }
    }
}
