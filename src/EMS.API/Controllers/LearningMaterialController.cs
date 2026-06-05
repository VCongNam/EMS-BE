using EMS.Application.Features.LearningMaterials.DTOs;
using EMS.Application.Features.LearningMaterials.Services;
using Microsoft.AspNetCore.Authorization;
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
            var id = await _materialService.CreateLearningMaterialAsync(request);
            return Ok(new { MaterialId = id, Message = "Learning material created successfully!" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLearningMaterial(Guid id, [FromForm] UpdateLearningMaterialDto request)
        {
            await _materialService.UpdateLearningMaterialAsync(id, request);
            return Ok(new { Message = "Learning material updated successfully!" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLearningMaterial(Guid id)
        {
            await _materialService.DeleteLearningMaterialAsync(id);
            return Ok(new { Message = "Learning material deleted successfully!" });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLearningMaterialDetail(Guid id)
        {
            var result = await _materialService.GetLearningMaterialDetailAsync(id);
            return Ok(result);
        }

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
            var result = await _studentMaterialService.GetClassMaterialsAsync(classId);
            return Ok(new
            {
                Message = "Lấy danh sách tài liệu thành công",
                Data = result
            });
        }
    }
}
