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
        public async Task<IActionResult> CreateClass([FromBody] CreateClassDto request)
        {
            var classId = await _classService.CreateClassAsync(request);
            return Ok(new { ClassId = classId, Message = "Class created successfully on Supabase!" });
        }

        [HttpGet("teacher/{teacherId}/dashboard")]
        public async Task<IActionResult> GetTeacherDashboard(Guid teacherId)
        {
            // Lưu ý: Thực tế teacherId thường được lấy từ Token JWT (User.Claims), 
            // nhưng tạm thời truyền qua URL để test cho dễ.
            var dashboardData = await _classService.GetTeacherDashboardAsync(teacherId);
            return Ok(dashboardData);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClassDetail(Guid id)
        {
            try
            {
                var classDetail = await _classService.GetClassDetailAsync(id);
                return Ok(classDetail);
            }
            catch (Exception ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }


        // API 3: Cập nhật thông tin lớp học
        // PUT: api/class/{id}[HttpPut("{id}")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClass(Guid id, [FromBody] UpdateClassDto request)
        {
            try
            {
                await _classService.UpdateClassAsync(id, request);
                return Ok(new { Message = "Class updated successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // API 4: Lưu trữ lớp học (Archive)
        // PATCH: api/class/{id}/archive[HttpPatch("{id}/archive")]
        [HttpPatch("{id}/archive")]
        public async Task<IActionResult> ArchiveClass(Guid id)
        {
            try
            {
                await _classService.ArchiveClassAsync(id);
                return Ok(new { Message = "Class archived successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

    }

}
