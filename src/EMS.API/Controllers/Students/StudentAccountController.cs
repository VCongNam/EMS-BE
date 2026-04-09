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
    public class StudentAccountController : ControllerBase
    {
        private readonly IStudentAccountService _studentAccountService;


        public StudentAccountController(IStudentAccountService studentAccountService)
        {
            _studentAccountService = studentAccountService;
        }

        [HttpPost("CreateStudentAccount")]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto request)
        {
            try
            {
                var newStudentId = await _studentAccountService.CreateStudentAsync(request);
                return StatusCode(201, new { Message = "Create student account successfully!", StudentId = newStudentId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            try
            {
                var result = await _studentAccountService.ImportStudentsFromExcelAsync(file);
                return Ok(result); 
            }
            catch (Exception ex)
            {
                // Lỗi này là lỗi hệ thống (ví dụ: mất kết nối DB, file hỏng), không phải lỗi data từng dòng
                return BadRequest(new { Message = ex.Message });
            }
        }
    }

}
}
