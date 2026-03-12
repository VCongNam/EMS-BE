using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Classes.Services;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClassController : ControllerBase
    {
        private readonly IClassService _classService;

        public ClassController(IClassService classService)
        {
            _classService = classService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
        {
            var classId = await _classService.CreateClassAsync(request);
            return Ok(new { ClassId = classId, Message = "Class created successfully on Supabase!" });
        }

        [HttpGet("{classId}/members")]
        public async Task<IActionResult> GetClassMember(Guid classId)
        {
            try
            {
                var members = await _classService.GetClassMembersAsync(classId);
                return Ok(new
                {
                    Message = "Get class members successfully",
                    TotalCount = members.Count(),
                    Data = members
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = "Error: " + ex.Message });
            }
        }
    }

}
