using EMS.Application.Features.LearningMaterials.DTOs;
using EMS.Application.Features.LearningMaterials.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LearningMaterialController : ControllerBase
    {
        private readonly ILearningMaterialService _materialService;
        private readonly IStudentMaterialService _studentMaterialService;

        public LearningMaterialController(ILearningMaterialService materialService, IStudentMaterialService studentMaterialService)
        {
            _materialService = materialService;
            _studentMaterialService = studentMaterialService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateLearningMaterial([FromForm] CreateLearningMaterialDto request)
        {
            try
            {
                var id = await _materialService.CreateLearningMaterialAsync(request);
                return Ok(new { MaterialId = id, Message = "Learning material created successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLearningMaterial(Guid id, [FromForm] UpdateLearningMaterialDto request)
        {
            try
            {
                await _materialService.UpdateLearningMaterialAsync(id, request);
                return Ok(new { Message = "Learning material updated successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLearningMaterial(Guid id)
        {
            try
            {
                await _materialService.DeleteLearningMaterialAsync(id);
                return Ok(new { Message = "Learning material deleted successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Xem chi tiết learning material (kèm attachments)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLearningMaterialDetail(Guid id)
        {
            try
            {
                var result = await _materialService.GetLearningMaterialDetailAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }

        // Xem toàn bộ learning materials của 1 lớp
        [HttpGet("class/{classId}")]
        public async Task<IActionResult> GetLearningMaterialsByClass(Guid classId)
        {
            var materials = await _materialService.GetLearningMaterialsByClassIdAsync(classId);
            return Ok(materials);
        }

        [HttpGet("student/{classId}/materials")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentClassMaterials(Guid classId)
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
