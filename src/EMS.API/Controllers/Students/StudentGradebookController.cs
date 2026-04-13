using EMS.Application.Features.Gradebook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers.Students
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentGradebookController : ControllerBase
    {
        private readonly IGradebookService _gradebookService;

        public StudentGradebookController(IGradebookService gradebookService)
        {
            _gradebookService = gradebookService;
        }

        [HttpGet("classes/{classId}/myGrades")]
        public async Task<IActionResult> GetMyGrades(Guid classId)
        {
            try
            {
                var result = await _gradebookService.GetStudentGradeReportAsync(classId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
