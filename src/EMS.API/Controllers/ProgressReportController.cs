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

        // --- 1. Tạo mới (Draft) ---
        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> CreateReport([FromBody] CreateProgressReportDto request)
        {
            try
            {
                var reportId = await reportService.CreateReportAsync(request);
                return Ok(new { Message = "Tạo báo cáo thành công", ReportId = reportId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // --- 2. Cập nhật bản nháp ---
        [HttpPut("{id}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> UpdateReport(Guid id, [FromBody] UpdateProgressReportDto request)
        {
            try
            {
                await reportService.UpdateReportAsync(id, request);
                return Ok(new { Message = "Cập nhật báo cáo thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // --- 3. Xóa báo cáo ---
        [HttpDelete("{id}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> DeleteReport(Guid id)
        {
            try
            {
                await reportService.DeleteReportAsync(id);
                return Ok(new { Message = "Xóa báo cáo thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // --- 4. Gửi báo cáo (Publish) ---
        [HttpPatch("{id}/send")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> SendReport(Guid id)
        {
            try
            {
                await reportService.SendReportAsync(id);
                return Ok(new { Message = "Đã gửi báo cáo thành công tới Phụ huynh/Học sinh!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // --- 5. Lấy chi tiết ---
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

        // --- 6. Dành cho Học sinh/Phụ huynh (Chỉ thấy Published) ---
        [HttpGet("student/{studentId}/class/{classId}")]
        public async Task<IActionResult> GetReportsForStudent(Guid studentId, Guid classId)
        {
            try
            {
                var reports = await reportService.GetReportsForStudentAsync(studentId, classId);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // --- 7. Dành cho Giáo viên (Quản lý toàn bộ 1 Lớp) ---
        [HttpGet("class/{classId}")]
        public async Task<IActionResult> GetReportsByClass(Guid classId)
        {
            try
            {
                var reports = await reportService.GetReportsByClassAsync(classId);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // --- 8. Dành cho Giáo viên (Dashboard tổng quan) ---
        [HttpGet("teacher/dashboard")]
        public async Task<IActionResult> GetTeacherDashboardReports()
        {
            try
            {
                var reports = await reportService.GetReportsByTeacherAsync();
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
