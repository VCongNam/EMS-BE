using EMS.Application.Features.Gradebook.DTOs;
using EMS.Application.Features.Gradebook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Added to force authentication
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
            try
            {
                var result = await _gradebookService.GetGradeCategoriesByClassAsync(classId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("classes/{classId}/categories")]
        public async Task<IActionResult> AddGradeCategory(Guid classId, [FromBody] CreateGradeCategoryDto request)
        {
            try
            {
                var id = await _gradebookService.AddGradeCategoryAsync(classId, request);
                return Ok(new { GradeCategoryId = id, Message = "Grade category added successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("classes/{classId}/categories")]
        public async Task<IActionResult> UpdateGradeCategory(Guid classId, [FromBody] UpdateGradeCategoryDto request)
        {
            try
            {
                await _gradebookService.UpdateGradeCategoryAsync(classId, request);
                return Ok(new { Message = "Grade category updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("classes/{classId}/categories/bulk-update")]
        public async Task<IActionResult> BulkUpdateGradeCategories(Guid classId, [FromBody] BulkUpdateGradeCategoryDto request)
        {
            try
            {
                await _gradebookService.BulkUpdateCategoriesAsync(classId, request);
                return Ok(new { Message = "Grade categories updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("classes/{classId}/categories/{categoryId}")]
        public async Task<IActionResult> DeleteGradeCategory(Guid classId, Guid categoryId)
        {
            try
            {
                await _gradebookService.DeleteGradeCategoryAsync(classId, categoryId);
                return Ok(new { Message = "Grade category deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("classes/{classId}")]
        public async Task<IActionResult> GetClassGradebook(Guid classId)
        {
            try
            {
                var result = await _gradebookService.GetClassGradebookAsync(classId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("classes/{classId}/export/excel")]
        public async Task<IActionResult> ExportGradebookToExcel(Guid classId)
        {
            try
            {
                var fileBytes = await _gradebookService.ExportClassGradebookToExcelAsync(classId);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Gradebook_Class_{classId}.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("classes/{classId}/export/pdf")]
        public async Task<IActionResult> ExportGradebookToPdf(Guid classId)
        {
            try
            {
                var fileBytes = await _gradebookService.ExportClassGradebookToPdfAsync(classId);
                return File(fileBytes, "application/pdf", $"Gradebook_Class_{classId}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }


        [HttpPut("class/{classId}/bulk-save")]
        public async Task<IActionResult> BulkSaveGrades(Guid classId, [FromBody] BulkSaveGradesRequest request)
        {
            try
            {
                await _gradebookService.SaveBulkGradesAsync(classId, request);
                return Ok(new { Message = "Gradebook saved successfully!" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }

        }

        [HttpGet("student/{classId}/myGrades")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentClassGrades(Guid classId)
        {
            try
            {
                var result = await _gradebookService.GetStudentGradeReportAsync(classId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}