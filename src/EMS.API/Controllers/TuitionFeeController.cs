using EMS.Application.Common.Interfaces;
using EMS.Application.Features.TuitionFees.Dtos;
using EMS.Application.Features.TuitionFees.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Teacher")]
    public class TuitionFeeController : ControllerBase
    {
        private readonly ITuitionFeeService tuitionFeeService;
        private readonly ICurrentUserService currentUserService;

        public TuitionFeeController(ITuitionFeeService tuitionFeeService, ICurrentUserService currentUserService)
        {
            this.tuitionFeeService = tuitionFeeService;
            this.currentUserService = currentUserService;
        }

        /// <summary>
        /// UC1: Lấy danh sách cấu hình học phí của tất cả các lớp do giáo viên này quản lý.
        /// </summary>
        [HttpGet("configs")]
        public async Task<IActionResult> GetConfigs()
        {
            try
            {
                var teacherId = currentUserService.UserId;
                var result = await tuitionFeeService.GetTuitionFeeConfigsAsync(teacherId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                // Do not expose internal exception messages to clients
                return StatusCode(500, new { Message = "Đã xảy ra lỗi nội bộ. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// UC1: Cập nhật đơn giá và hình thức thu (Thu trước/Thu sau) cho 1 lớp.
        /// </summary>
        [HttpPut("class/{classId}/fee")]
        public async Task<IActionResult> UpdateFee(Guid classId, UpdateTuitionFeeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var teacherId = currentUserService.UserId;
                await tuitionFeeService.UpdateTuitionFeeAsync(classId, dto, teacherId);
                return Ok(new { Message = "Cập nhật cấu hình học phí thành công." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                // validation/business rule error: return client-friendly message
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi nội bộ. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// UC5 & UC2: Phát hành hóa đơn hàng loạt cho cả lớp.
        /// </summary>
        [HttpPost("class/{classId}/generate-invoices")]
        public async Task<IActionResult> Generate(Guid classId, GenerateInvoiceDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var teacherId = currentUserService.UserId;
                await tuitionFeeService.GenerateInvoicesForClassAsync(classId, dto, teacherId);
                return Ok(new { Message = "Đã phát hành hóa đơn cho kỳ này thành công." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi nội bộ. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Tính năng Cấn trừ: Chốt sổ cuối tháng để tính tiền hoàn lại cho học sinh.
        /// </summary>
        [HttpPost("class/{classId}/reconcile")]
        public async Task<IActionResult> ReconcilePrepaid(Guid classId, [FromQuery] int month, [FromQuery] int year)
        {
            try
            {
                var teacherId = currentUserService.UserId;
                await tuitionFeeService.ReconcilePrepaidClassAsync(classId, month, year, teacherId);
                return Ok(new { Message = "Đã chốt sổ và cộng tiền cấn trừ cho tháng sau." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi nội bộ. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// UC6: Lấy danh sách các giao dịch (hóa đơn chuyển khoản) đang chờ xử lý của các lớp giáo viên đang dạy.
        /// </summary>
        [HttpGet("transactions/pending")]
        public async Task<IActionResult> GetPending()
        {
            try
            {
                var teacherId = currentUserService.UserId;
                var result = await tuitionFeeService.GetPendingTransactionsAsync(teacherId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi nội bộ. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Lấy danh sách hóa đơn của một lớp trong kỳ với paging và filter
        /// </summary>
        [HttpGet("class/{classId}/invoices")]
        public async Task<IActionResult> GetClassInvoices(Guid classId,
            [FromQuery] int? month,
            [FromQuery] int? year,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20,
            [FromQuery] string? status = null,
            [FromQuery] Guid? studentId = null)
        {
            try
            {
                var teacherId = currentUserService.UserId;
                int m = month ?? DateTime.UtcNow.Month;
                int y = year ?? DateTime.UtcNow.Year;

                var (items, total) = await tuitionFeeService.GetClassInvoicesForPeriodAsync(classId, m, y, teacherId, page, size, status, studentId);

                return Ok(new { Items = items, TotalCount = total, Page = page, Size = size });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi nội bộ. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// UC7: Phê duyệt hoặc Từ chối một giao dịch chuyển khoản.
        /// </summary>
        [HttpPost("transaction/{id}/review")]
        public async Task<IActionResult> Review(Guid id, [FromBody] ReviewTransactionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await tuitionFeeService.ReviewTransactionAsync(id, dto.IsApproved, currentUserService.UserId, dto.Note);
                return Ok(new { Message = "Đã xử lý giao dịch thành công." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi nội bộ. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// UC4: Lấy chi tiết báo cáo công nợ của từng học sinh trong một lớp.
        /// </summary>
        [HttpGet("class/{classId}/detail")]
        public async Task<IActionResult> GetDetail(Guid classId, [FromQuery] int month, [FromQuery] int year)
        {
            try
            {
                var teacherId = currentUserService.UserId;
                var result = await tuitionFeeService.GetClassFinancialDetailAsync(classId, month, year, teacherId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// UC3: Lấy báo cáo doanh thu tổng quan của riêng giáo viên đó.
        /// </summary>
        [HttpGet("report/summary")]
        public async Task<IActionResult> GetReportSummary()
        {
            try
            {
                var teacherId = currentUserService.UserId;
                var result = await tuitionFeeService.GetOverallReportAsync(teacherId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("report/class-summaries")]
        public async Task<IActionResult> GetClassFinancialSummaries()
        {
            try
            {
                var teacherId = currentUserService.UserId;
                var result = await tuitionFeeService.GetClassFinancialSummariesAsync(teacherId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("report/dashboard-analytics")]
        public async Task<IActionResult> GetDashboardAnalytics()
        {
            try
            {
                var teacherId = currentUserService.UserId;
                var result = await tuitionFeeService.GetDashboardAnalyticsAsync(teacherId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("invoice/{invoiceId}/extend-due-date")]
        public async Task<IActionResult> ExtendDueDate(Guid invoiceId, [FromBody] ExtendInvoiceDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var teacherId = currentUserService.UserId;
                await tuitionFeeService.ExtendInvoiceDueDateAsync(invoiceId, dto.AdditionalDays, teacherId);
                return Ok(new { Message = $"Đã gia hạn hóa đơn thêm {dto.AdditionalDays} ngày." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Gia hạn hàng loạt tất cả hóa đơn đang nợ của một lớp trong một kỳ cụ thể
        /// </summary>
        [HttpPut("class/{classId}/extend-due-date")]
        public async Task<IActionResult> ExtendClassDueDate(Guid classId, [FromBody] ExtendClassInvoicesDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var teacherId = currentUserService.UserId;
                await tuitionFeeService.ExtendClassInvoicesDueDateAsync(classId, dto, teacherId);
                return Ok(new { Message = $"Đã gia hạn thành công thêm {dto.AdditionalDays} ngày cho các hóa đơn đang nợ của lớp." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}