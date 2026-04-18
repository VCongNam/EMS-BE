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
        public async Task<IActionResult> GetTeacherGrowthReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {

            var result = await reportService.GetGrowthReportAsync(startDate, endDate);
            return Ok(new
            {
                StatusCode = 200,
                Message = "Lấy dữ liệu báo cáo thành công.",
                Data = result
            });
        }
    }
}
