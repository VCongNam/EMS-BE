using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Classes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClassController : ControllerBase
    {
        private readonly IClassService _classService;
        private readonly IClassTAService _classTAService;

        public ClassController(IClassService classService, IClassTAService classTAService)
        {
            _classService = classService;
            _classTAService = classTAService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassDto request)
        {
            var classId = await _classService.CreateClassAsync(request);
            return Ok(new { ClassId = classId, Message = "Class created successfully on Supabase!" });
        }

        [HttpGet("{classId}/members")]
        [Authorize]
        public async Task<IActionResult> GetClassMember(Guid classId)
        {
            try
            {
                var members = await _classService.GetClassMembersAsync(classId);
                return Ok(new
                {
                    Message = "Get class members successfully!",
                    TotalCount = members.Count(),
                    Data = members
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = "Error: " + ex.Message });
            }
        }
        [HttpPost("{classId}/assignStudent")]
        [Authorize]
        public async Task<IActionResult> AssignStudent(Guid classId, [FromBody] AssignStudentDto request)
        {
            try
            {
                await _classService.AssignStudentAsync(classId, request);
                return StatusCode(201, new { Message = "Student added successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
        [HttpGet("teacher/dashboard")]
        [Authorize]
        public async Task<IActionResult> GetTeacherDashboard()
        {
            var dashboardData = await _classService.GetTeacherDashboardAsync();
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

        [HttpGet("my-id")]
        [Authorize]
        public IActionResult GetMyId()
        {
            // Thông tin này được trích xuất tự động từ Token bạn gửi lên
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            return Ok(new { UserId = userId, Email = email, Role = role });
        }

        [HttpGet("{classId}/tas")]
        [Authorize]
        public async Task<IActionResult> GetClassTAs(Guid classId)
        {
            var tas = await _classTAService.GetClassTAsAsync(classId);
            return Ok(new { Data = tas });
        }

        [HttpPost("{classId}/tas/assign")]
        [Authorize]
        public async Task<IActionResult> AssignTA(Guid classId, [FromBody] AssignTADto request)
        {
            await _classTAService.AssignTAAsync(classId, request);
            return StatusCode(201, new { Message = "Phân công trợ giảng thành công" });
        }

        [HttpPut("{classId}/tas/{taId}/permission")]
        [Authorize]
        public async Task<IActionResult> SetTAPermisson(Guid classId, Guid taId, [FromBody] UpdateTAPermissionDto request)
        {
            await _classTAService.UpdateTAPermissionAsync(classId, taId, request);
            return Ok(new { Message = "Cập nhật quyền hạn Trợ giảng thành công!" });
        }
    }

}
