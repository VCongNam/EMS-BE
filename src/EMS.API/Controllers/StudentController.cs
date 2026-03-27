using EMS.Application.Features.Students.DTOs;
using EMS.Application.Features.Students.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpPost("CreateStudentAccount")]
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

        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyClasses([FromQuery] EnrolledClassFilter filter)
        {
            var result = await _studentService.GetMyClassesAsync(filter);
            return Ok(new
            {
                Message = "Lấy danh sách lớp học thành công",
                Dât = result
            });
        }
    }
}
