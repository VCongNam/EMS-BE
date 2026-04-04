using EMS.Application.Features.ProgressReports.DTOs;
using EMS.Application.Features.ProgressReports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Yêu cầu người dùng phải đăng nhập (có token JWT)
    public class ProgressReportController : ControllerBase
    {
        private readonly IProgressReportService reportService;

        public ProgressReportController(IProgressReportService reportService)
        {
            this.reportService = reportService;
        }

        // --- MÀN HÌNH 2: Lấy danh sách học sinh của một lớp kèm trạng thái báo cáo ---
        // GET: api/ProgressReport/class/{classId}?month=3&year=2026
        [HttpGet("class/{classId}")]
        public async Task<IActionResult> GetClassReportDetails(Guid classId, [FromQuery] int month, [FromQuery] int year)
        {
            var result = await reportService.GetClassReportDetailsAsync(classId, month, year);
            return Ok(result);
        }

        // --- MÀN HÌNH 3: Lấy chi tiết báo cáo để đổ dữ liệu lên form sửa ---
        // GET: api/ProgressReport/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReportDetail(Guid id)
        {
            try
            {
                var report = await reportService.GetReportDetailAsync(id);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        // --- MÀN HÌNH 3: Tạo mới báo cáo (Lưu nháp hoặc Gửi ngay) ---
        // POST: api/ProgressReport
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateProgressReportDto request)
        {
            try
            {
                var reportId = await reportService.CreateReportAsync(request);

                // Trả về câu thông báo tùy theo trạng thái
                string message = request.Status == "Published" ? "Đã gửi báo cáo thành công!" : "Lưu nháp thành công.";
                return Ok(new { Message = message, ReportId = reportId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // --- MÀN HÌNH 3: Cập nhật báo cáo đã tạo (Lưu tiếp nháp hoặc Chốt gửi) ---
        // PUT: api/ProgressReport/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReport(Guid id, [FromBody] UpdateProgressReportDto request)
        {
            try
            {
                await reportService.UpdateReportAsync(id, request);

                string message = request.Status == "Published" ? "Đã gửi báo cáo thành công!" : "Cập nhật bản nháp thành công.";
                return Ok(new { Message = message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // --- MÀN HÌNH 2: Xóa báo cáo (Chỉ áp dụng cho bản nháp) ---
        // DELETE: api/ProgressReport/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReport(Guid id)
        {
            try
            {
                await reportService.DeleteReportAsync(id);
                return Ok(new { Message = "Xóa báo cáo thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
