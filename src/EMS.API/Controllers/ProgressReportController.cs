using EMS.Application.Features.ProgressReports.DTOs;
using EMS.Application.Features.ProgressReports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bắt buộc phải đăng nhập (Có JWT)
    public class ProgressReportController : ControllerBase
    {
        private readonly IProgressReportService reportService;

        public ProgressReportController(IProgressReportService reportService)
        {
            this.reportService = reportService;
        }

        // Chức năng: Create Progress Report
        [HttpPost]
        public async Task<IActionResult> CreateProgressReport([FromBody] CreateProgressReportDto request)
        {
            try
            {
                var reportId = await reportService.CreateReportAsync(request);
                return StatusCode(201, new { Message = "Progress report created successfully!", ReportId = reportId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Chức năng: View Teaching Dashboard (Lấy list report của giáo viên)
        [HttpGet("my-reports")]
        public async Task<IActionResult> GetMyTeachingReports()
        {
            try
            {
                var reports = await reportService.GetMyTeachingReportsAsync();
                return Ok(new { Message = "Success", Data = reports });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Chức năng: View Detail
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReportDetail(Guid id)
        {
            try
            {
                var report = await reportService.GetReportByIdAsync(id);
                return Ok(new { Message = "Success", Data = report });
            }
            catch (Exception ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }

        // Chức năng: Update Progress Report
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProgressReport(Guid id, [FromBody] UpdateProgressReportDto request)
        {
            try
            {
                await reportService.UpdateReportAsync(id, request);
                return Ok(new { Message = "Progress report updated successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Chức năng: Delete Progress Report
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProgressReport(Guid id)
        {
            try
            {
                await reportService.DeleteReportAsync(id);
                return Ok(new { Message = "Progress report deleted successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Chức năng: Send Progress Report (Non-UI)
        [HttpPost("{id}/send")]
        public async Task<IActionResult> SendProgressReport(Guid id)
        {
            try
            {
                await reportService.SendReportAsync(id);
                return Ok(new { Message = "Progress report sent successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
