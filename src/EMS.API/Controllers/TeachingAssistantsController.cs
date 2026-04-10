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
    }
}
