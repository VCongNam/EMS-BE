using EMS.Application.Features.ProgressReports.DTOs;
using EMS.Application.Features.ProgressReports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProgressReportController : ControllerBase
    {
        private readonly IProgressReportService reportService;

        public ProgressReportController(IProgressReportService reportService)
        {
            this.reportService = reportService;
        }

        [HttpGet("class/{classId}")]
        public async Task<IActionResult> GetClassReportDetails(Guid classId, [FromQuery] int month, [FromQuery] int year)
        {
            var result = await reportService.GetClassReportDetailsAsync(classId, month, year);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReportDetail(Guid id)
        {
            return Ok(await reportService.GetReportDetailAsync(id)); 
        }

        [Authorize(Roles ="Teacher")]
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateProgressReportDto request)
        {
                var reportId = await reportService.CreateReportAsync(request);
                string message = request.Status == "Published" ? "Đã chốt và gửi báo cáo!" : "Đã lưu nháp cùng dữ liệu mới nhất.";
                return Ok(new { Message = message, ReportId = reportId });
        }


        [Authorize(Roles = "Teacher")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReport(Guid id, [FromBody] UpdateProgressReportDto request)
        {
                await reportService.UpdateReportAsync(id, request);
                string message = request.Status == "Published" ? "Đã chốt và gửi báo cáo!" : "Cập nhật bản nháp thành công.";
                return Ok(new { Message = message });

        }

        [Authorize(Roles = "Teacher")]
        [HttpPut("{id}/send")]
        public async Task<IActionResult> SendReport(Guid id)
        {
                await reportService.SendReportAsync(id);
                return Ok(new { Message = "Báo cáo đã được chốt và gửi thành công." });

        }


        [Authorize(Roles = "Teacher")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReport(Guid id)
        {
                await reportService.DeleteReportAsync(id);
                return Ok(new { Message = "Xóa báo cáo nháp thành công." });
        }
        
        [Authorize(Roles = "Teacher")]
        [HttpGet("classes/summary")]
        public async Task<IActionResult> GetClassesReportSummary(
     [FromQuery] int month,
     [FromQuery] int year,
     [FromQuery] string? search)
        {
                var result = await reportService.GetClassesSummaryAsync(month, year, search);
                return Ok(result);
 
        }
    }
}
