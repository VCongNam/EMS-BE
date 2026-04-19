using EMS.Application.Features.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/reports/teachers")]
    [Authorize(Roles = "Teacher")]
    public class TeacherReportController : ControllerBase
    {
        private readonly ITeacherReportService reportService;
        public TeacherReportController(ITeacherReportService reportService)
        {
            this.reportService = reportService;

        }

        [HttpGet("growth")]
        public async Task<IActionResult> GetTeacherGrowthReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] Guid? subjectId,
            [FromQuery] string? status)
        {
            var result = await reportService.GetGrowthReportAsync(startDate, endDate, subjectId, status);
            return Ok(result);
        }


        [HttpGet("classes/{classId}/growth")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetSingleClassGrowthReport(
            Guid classId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var result = await reportService.GetSingleClassGrowthReportAsync(classId, startDate, endDate);

            return Ok(new
            {
                StatusCode = 200,
                Message = "Lấy báo cáo chi tiết lớp học thành công.",
                Data = result
            });
        }
    }
}
