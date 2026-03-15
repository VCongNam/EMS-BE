using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Classes.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto request)
        {
            try
            {
                var newStudentId = await _studentService.CreateStudentAsync(request);
                return StatusCode(201, new { Message = "Create student account successfully!", StudentId = newStudentId });
            }
            catch (Exception ex)
            {
                
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
