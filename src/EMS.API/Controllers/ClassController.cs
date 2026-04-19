using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Classes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClassController : ControllerBase
    {
        private readonly IClassService _classService;
        private readonly IClassTAService _classTAService;
        private readonly IStudentClassService _studentClassService;

        public ClassController(IClassService classService, IClassTAService classTAService, IStudentClassService studentClassService)
        {
            _classService = classService;
            _classTAService = classTAService;
            _studentClassService = studentClassService;
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


        [HttpGet("{classId}/staff")]
        public async Task<IActionResult> GetClassStaff(Guid classId)
        {
            var staff = await _classService.GetClassStaffOnlyAsync(classId);
            return Ok(staff);
        }

        [HttpPost("{classId}/assignStudent")]
        [Authorize(Roles ="Teacher")]
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

        [HttpPost("{classId}/assignMultipleStudent")]
        [Authorize(Roles ="Teacher")]
        public async Task<IActionResult> AssignMultipleStudent(Guid classId, [FromBody] AssignMultipleStudentsDto request)
        {
            try
            {
                if (request.StudentIds == null || request.StudentIds.Count == 0)
                {
                    return BadRequest(new { message = "Danh sách học sinh không được để trống." });
                }

                var result = await _classService.AssignMultipleStudentsAsync(classId, request.StudentIds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("teacher/dashboard")]
        [Authorize]
        public async Task<IActionResult> GetTeacherDashboard()
        {
            var dashboardData = await _classService.GetTeacherDashboardAsync();
            return Ok(dashboardData);
        }

        [HttpGet("teacher/archived-classes")]
        [Authorize]
        public async Task<IActionResult> GetArchivedClasses()
        {
            var archivedClasses = await _classService.GetArchivedClassesAsync();
            return Ok(archivedClasses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClassDetail(Guid id)
        {
           
                var classDetail = await _classService.GetClassDetailAsync(id);
                return Ok(classDetail);
            
           
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
                await _classService.ArchiveClassAsync(id);
                return Ok(new { Message = "Class archived successfully!" });           
          
        }

        // API 5: Khôi phục lớp học (Restore)
        // PATCH: api/class/{id}/restore
        [HttpPatch("{id}/restore")]
        public async Task<IActionResult> RestoreClass(Guid id)
        {
          
                await _classService.RestoreClassAsync(id);
                return Ok(new { Message = "Class restored successfully!" });
            
          
        }

        [HttpGet("my-id")]
        [Authorize]
        public IActionResult GetMyId()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            return Ok(new { UserId = userId, Email = email, Role = role });
        }

        [HttpGet("{classId}/tas")]
        [Authorize]
        public async Task<IActionResult> GetClassTAs(Guid classId)
        {
            try
            {
                var tas = await _classTAService.GetClassTAsAsync(classId);
                return Ok(new { Data = tas });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{classId}/tas/assign")]
        [Authorize]
        public async Task<IActionResult> AssignTA(Guid classId, [FromBody] AssignTADto request)
        {
            try
            {
                await _classTAService.AssignTAAsync(classId, request);
                return StatusCode(201, new { Message = "Phân công trợ giảng thành công" });
            }
            catch(Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{classId}/tas/{taId}/permission")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> SetTAPermisson(Guid classId, Guid taId, [FromBody] UpdateTAPermissionDto request)
        {
            try
            {
                await _classTAService.UpdateTAPermissionAsync(classId, taId, request);
                return Ok(new { Message = "Cập nhật quyền hạn Trợ giảng thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("createTask")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto request)
        {
            try
            {
                var task = await _classTAService.CreateTaskAsync(request);
                return StatusCode(201, new { Message = "Giao việc thành công", TaskId = task });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("classta/{classTaId}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetAssignedTasks(Guid classTaId)
        {
            var tasks = await _classTAService.GetTasksAsync(classTaId);
            return Ok(new { Data = tasks });
        }

        [Authorize(Roles = "Teacher")]
        [HttpPut("{classId}/students/{studentId}/remove")]
        public async Task<IActionResult> RemoveStudent(Guid classId, Guid studentId)
        {
            try
            {
                await _classService.RemoveStudentFromClassAsync(classId, studentId);
                return Ok(new { Message = "Đã đẩy học sinh ra khỏi lớp thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize(Roles = "Teacher")]
        [HttpPut("{classId}/students/{studentId}/restore")]
        public async Task<IActionResult> RestoreStudent(Guid classId, Guid studentId)
        {
            try
            {
                await _classService.RestoreStudentInClassAsync(classId, studentId);
                return Ok(new { Message = "Đã khôi phục trạng thái học sinh thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // API: Lấy tất cả công việc của một Trợ giảng (tổng hợp từ các lớp)
        [HttpGet("ta/{taId}/tasks")]
        [Authorize]
        public async Task<IActionResult> GetTATasks(Guid taId)
        {
            try
            {
                var tasks = await _classTAService.GetTasksByTAIdAsync(taId);
                return Ok(new { Message = "Lấy danh sách công việc thành công!", Data = tasks });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // API: Lấy danh sách các lớp mà Trợ giảng này đang tham gia
        [HttpGet("ta/{taId}/classes")]
        [Authorize]
        public async Task<IActionResult> GetTAAssignedClasses(Guid taId)
        {
            try
            {
                var classes = await _classTAService.GetClassesByTAIdAsync(taId);
                return Ok(new { Message = "Lấy danh sách lớp học thành công!", Data = classes });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{classId}/tas/{taId}/remove")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> RemoveTAFromClass(Guid classId, Guid taId)
        {
            try
            {
                await _classTAService.RemoveTAFromClassAsync(classId, taId);
                return Ok(new { message = "Đã gỡ trợ giảng khỏi lớp thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //Student 
        [HttpGet("student/myClasses")]
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
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }

        }

        [HttpGet("student/{classId}/detail")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentClassDetail(Guid classId)
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

        [HttpGet("my-students")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetMyStudents()
        {
            try
            {
                var result = await _studentClassService.GetStudentsManagedByTeacherAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Lỗi khi lấy danh sách học sinh: " + ex.Message });
            }
        }
    }

}
