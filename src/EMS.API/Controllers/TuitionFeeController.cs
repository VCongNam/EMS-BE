using EMS.Application.Common.Interfaces;
using EMS.Application.Features.TuitionFees.Dtos;
using EMS.Application.Features.TuitionFees.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.NetworkInformation;
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

        

       





      

        // Phát hành hóa đơn cho một lớp trong một kỳ
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

        // Chốt sổ cuối tháng để trừ/cộng dồn cho tháng sau (áp dụng cho các lớp có học phí trả trước)
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


        // =========================================================
        // 🔍 MÀN 3: DUYỆT GIAO DỊCH (Queue & History)
        // =========================================================

        // Lấy danh sách các giao dịch chuyển khoản đang chờ phê duyệt của giáo viên
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

        [HttpGet("transactions/history")]
        public async Task<IActionResult> GetHistory([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var result = await tuitionFeeService.GetTransactionHistoryAsync(currentUserService.UserId, fromDate, toDate);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Lỗi khi tải lịch sử giao dịch." });
            }
        }





        [HttpGet("class/{classId}/postpaid-invoices")]
        public async Task<IActionResult> GetPostpaidInvoices(Guid classId, [FromQuery] int month, [FromQuery] int year)
        {
            try
            {
                // Gọi cực kỳ sạch sẽ, không cần bận tâm lấy UserId nữa
                var result = await tuitionFeeService.GetPostpaidInvoicesAsync(classId, month, year);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { Message = "Lỗi hệ thống." }); }
        }

        [HttpGet("class/{classId}/prepaid-invoices")]
        public async Task<IActionResult> GetPrepaidInvoices(Guid classId, [FromQuery] int month, [FromQuery] int year)
        {
            try
            {
                var result = await tuitionFeeService.GetPrepaidInvoicesAsync(classId, month, year);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { Message = "Lỗi hệ thống." }); }
        }

        [HttpGet("class/{classId}/revenue-summary")]
        public async Task<IActionResult> GetClassRevenueSummary(Guid classId, [FromQuery] int month, [FromQuery] int year)
        {
            try
            {
                var result = await tuitionFeeService.GetClassRevenueReportAsync(classId, month, year);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception) { return StatusCode(500, new { Message = "Lỗi hệ thống." }); }
        }

        /// <summary>
        /// Lấy danh sách cấu hình học phí của tất cả các lớp đang dạy
        /// </summary>
        [HttpGet("configs")]
        public async Task<IActionResult> GetFeeConfigs()
        {
            try
            {
                var result = await tuitionFeeService.GetClassFeeConfigsAsync();
                return Ok(result); // Bây giờ nó trả thẳng về List<ClassFeeConfigDto>
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception) { return StatusCode(500, new { Message = "Lỗi hệ thống khi tải cấu hình." }); }
        }

        /// <summary>
        /// Cập nhật cấu hình học phí cho một lớp cụ thể
        /// </summary>
        [HttpPut("class/{classId}/config")]
        public async Task<IActionResult> UpdateClassConfig(Guid classId, [FromBody] UpdateClassFeeConfigDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await tuitionFeeService.UpdateClassFeeAsync(classId, dto);
                return Ok(new { Message = "Đã cập nhật cấu hình lớp học thành công." });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { Message = "Lỗi hệ thống." }); }
        }

        /// <summary>
        /// Lấy chi tiết cấu hình học phí của MỘT lớp cụ thể (Dùng để fill dữ liệu vào Form Edit)
        /// </summary>
        [HttpGet("class/{classId}/config")]
        public async Task<IActionResult> GetClassConfig(Guid classId)
        {
            try
            {
                var result = await tuitionFeeService.GetClassFeeConfigAsync(classId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi tải cấu hình lớp." });
            }
        }

        /// <summary>
        /// Gia hạn thêm ngày cho MỘT hóa đơn cụ thể (Dùng khi 1 phụ huynh xin khất)
        /// </summary>
        [HttpPut("invoice/{invoiceId}/extend-due-date")]
        public async Task<IActionResult> ExtendSingleInvoice(Guid invoiceId, [FromBody] ExtendInvoiceDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await tuitionFeeService.ExtendInvoiceAsync(invoiceId, dto);
                return Ok(new { Message = $"Đã gia hạn hóa đơn thêm {dto.AdditionalDays} ngày thành công." });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (KeyNotFoundException ex) { return NotFound(new { Message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { Message = "Lỗi hệ thống." }); }
        }

        /// <summary>
        /// Gia hạn hàng loạt cho TẤT CẢ hóa đơn đang nợ của một lớp trong một kỳ
        /// </summary>
        [HttpPut("class/{classId}/extend-due-date")]
        public async Task<IActionResult> ExtendClassInvoices(Guid classId, [FromBody] ExtendClassInvoicesDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await tuitionFeeService.ExtendClassInvoicesAsync(classId, dto);
                return Ok(new { Message = $"Đã gia hạn thành công thêm {dto.AdditionalDays} ngày cho toàn bộ hóa đơn đang nợ của lớp." });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { Message = "Lỗi hệ thống." }); }
        }



    }
}