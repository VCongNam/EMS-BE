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
    }

}
