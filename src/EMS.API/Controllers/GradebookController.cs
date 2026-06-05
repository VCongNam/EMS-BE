using EMS.Application.Features.Gradebook.DTOs;
using EMS.Application.Features.Gradebook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GradebookController : ControllerBase
    {
        private readonly IGradebookService _gradebookService;

        public GradebookController(IGradebookService gradebookService)
        {
            _gradebookService = gradebookService;
        }

        [HttpGet("classes/{classId}/categories")]
        public async Task<IActionResult> GetGradeCategories(Guid classId)
        {
            var result = await _gradebookService.GetGradeCategoriesByClassAsync(classId);
            return Ok(result);
        }

        [HttpPost("classes/{classId}/categories")]
        public async Task<IActionResult> AddGradeCategory(Guid classId, [FromBody] CreateGradeCategoryDto request)
        {
            var id = await _gradebookService.AddGradeCategoryAsync(classId, request);
            return Ok(new { GradeCategoryId = id, Message = "Grade category added successfully" });
        }

        [HttpPut("classes/{classId}/categories")]
        public async Task<IActionResult> UpdateGradeCategory(Guid classId, [FromBody] UpdateGradeCategoryDto request)
        {
            await _gradebookService.UpdateGradeCategoryAsync(classId, request);
            return Ok(new { Message = "Grade category updated successfully" });
        }

        [HttpPut("classes/{classId}/categories/bulk-update")]
        public async Task<IActionResult> BulkUpdateGradeCategories(Guid classId, [FromBody] BulkUpdateGradeCategoryDto request)
        {
            await _gradebookService.BulkUpdateCategoriesAsync(classId, request);
            return Ok(new { Message = "Grade categories updated successfully" });
        }

        [HttpDelete("classes/{classId}/categories/{categoryId}")]
        public async Task<IActionResult> DeleteGradeCategory(Guid classId, Guid categoryId)
        {
            await _gradebookService.DeleteGradeCategoryAsync(classId, categoryId);
            return Ok(new { Message = "Grade category deleted successfully" });
        }

        [HttpGet("classes/{classId}")]
        public async Task<IActionResult> GetClassGradebook(Guid classId)
        {
            var result = await _gradebookService.GetClassGradebookAsync(classId);
            return Ok(result);
        }

        [HttpGet("classes/{classId}/export/excel")]
        public async Task<IActionResult> ExportGradebookToExcel(Guid classId)
        {
            var fileBytes = await _gradebookService.ExportClassGradebookToExcelAsync(classId);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Gradebook_Class_{classId}.xlsx");
        }

        [HttpGet("classes/{classId}/export/pdf")]
        public async Task<IActionResult> ExportGradebookToPdf(Guid classId)
        {
            var fileBytes = await _gradebookService.ExportClassGradebookToPdfAsync(classId);
            return File(fileBytes, "application/pdf", $"Gradebook_Class_{classId}.pdf");
        }

        [HttpPut("class/{classId}/bulk-save")]
        public async Task<IActionResult> BulkSaveGrades(Guid classId, [FromBody] BulkSaveGradesRequest request)
        {
            await _gradebookService.SaveBulkGradesAsync(classId, request);
            return Ok(new { Message = "Gradebook saved successfully!" });
        }

        [HttpGet("student/{classId}/myGrades")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentClassGrades(Guid classId)
        {
            var result = await _gradebookService.GetStudentGradeReportAsync(classId);
            return Ok(result);
        }
    }
}
