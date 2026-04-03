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
    public class StudentClassController : ControllerBase
    {
        private readonly IStudentClassService _studentClassService;
        private readonly IStudentMaterialService _studentMaterialService;
        public StudentClassController(IStudentClassService studentClassService, IStudentMaterialService studentMaterialService)
        {
            _studentClassService = studentClassService;
            _studentMaterialService = studentMaterialService;
        }

        [HttpGet("MyClasses")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyClasses([FromQuery] EnrolledClassFilter filter)
        {
            try
            {
                var result = await _studentClassService.GetMyClassesAsync(filter);
                return Ok(new
                {
                    Message = "Lấy danh sách lớp học thành công",
                    Data = result
                });
            }
            catch (Exception ex) {
                return BadRequest(new { Error = ex.Message });
            }
            
        }

        [HttpGet("{classId}/Detail")]
        public async Task<IActionResult> GetClassDetail(Guid classId)
        {
            try
            {
                var result = await _studentClassService.GetClassDetailAsync(classId);
                return Ok(new
                {
                    Message = "Lấy thông tin lớp học thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("{classId}/Posts")]
        public async Task<IActionResult> GetClassPosts(Guid classId, [FromQuery] PostFilter filter)
        {
            try
            {
                var result = await _studentClassService.GetClassPostsAsync(classId, filter);
                return Ok(new
                {
                    Message = "Lấy bảng tin thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("{classId}/materials")]
        public async Task<IActionResult> GetClassMaterials(Guid classId)
        {
            try
            {
                var result = await _studentMaterialService.GetClassMaterialsAsync(classId);
                return Ok(new
                {
                    Message = "Lấy danh sách tài liệu thành công",
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
