using EMS.Application.Features.Students.DTOs;
using EMS.Application.Features.Students.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers.Students
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentTuitionController : ControllerBase
    {
        private readonly IStudentTuitionService _tuitionService;
        public StudentTuitionController(IStudentTuitionService tuitionService)
        {
            _tuitionService = tuitionService;
        }

        [HttpGet("tuitions")]
        public async Task<IActionResult> GetTuitions([FromQuery] TuitionFilter filter)
        {
            try
            {
                var result = await _tuitionService.GetMyTuitionAsync(filter);
                return Ok(new
                {
                    Message = "Lấy danh sách học phí thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }


    }
}
