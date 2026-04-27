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
    [Authorize]
    public class TuitionFeeController : ControllerBase
    {
        private readonly ITuitionFeeService tuitionFeeService;
        private readonly IStudentTuitionService _tuitionService;

        public TuitionFeeController(ITuitionFeeService tuitionFeeService, IStudentTuitionService tuitionService)
        {
            this.tuitionFeeService = tuitionFeeService;
            this._tuitionService = tuitionService;
        }

        [HttpGet("class/{classId}/preview-invoices")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> PreviewInvoices(Guid classId, [FromQuery] int month, [FromQuery] int year)
        {
            var result = await tuitionFeeService.GetInvoicesPreviewAsync(classId, month, year);
            return Ok(result);
        }

        [HttpPost("class/{classId}/confirm-invoices")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ConfirmInvoices(Guid classId, [FromBody] ConfirmInvoicesDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await tuitionFeeService.ConfirmAndGenerateInvoicesAsync(classId, dto);
                return Ok(new { Message = "Đã chốt và phát hành hóa đơn thành công." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { Message = "Đã xảy ra lỗi nội bộ." }); }
        }

        [HttpGet("class/{classId}/student/{studentId}/preview-final-invoice")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> PreviewFinalInvoice(Guid classId, Guid studentId, [FromQuery] int month, [FromQuery] int year)
        {
            var result = await tuitionFeeService.GetStudentFinalInvoicePreviewAsync(classId, studentId, month, year);
            return Ok(result);
        }

        [HttpPost("class/{classId}/student/{studentId}/confirm-final-invoice")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ConfirmFinalInvoice(Guid classId, Guid studentId, [FromBody] ConfirmSingleInvoiceDto dto)
        {
            return Ok(new { Message = "Đã phát hành hóa đơn tất toán cho học sinh thành công." });
        }

        // --- ENDPOINT QUẢN LÝ GIAO DỊCH ---

        [HttpGet("transactions/pending")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetPending()
        {
            var result = await tuitionFeeService.GetPendingTransactionsAsync();
            return Ok(result);
        }

        [HttpPost("transaction/{id}/review")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Review(Guid id, [FromBody] ReviewTransactionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await tuitionFeeService.ReviewTransactionAsync(id, dto.IsApproved, dto.Note);
                return Ok(new { Message = "Đã xử lý giao dịch thành công." });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception) { return StatusCode(500, new { Message = "Đã xảy ra lỗi nội bộ." }); }
        }

        [HttpGet("transactions/history")]
        public async Task<IActionResult> GetHistory([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var result = await tuitionFeeService.GetTransactionHistoryAsync(fromDate, toDate);
            return Ok(result);
        }

        [HttpGet("invoices/report")]
        public async Task<IActionResult> GetInvoicesReport([FromQuery] Guid? classId, [FromQuery] int month, [FromQuery] int year)
        {
            var result = await tuitionFeeService.GetInvoicesListAsync(classId, month, year);
            return Ok(result);
        }

        [HttpGet("configs")]
        public async Task<IActionResult> GetFeeConfigs()
        {
            var result = await tuitionFeeService.GetClassFeeConfigsAsync();
            return Ok(result);
        }

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

        [HttpGet("class/{classId}/config")]
        public async Task<IActionResult> GetClassConfig(Guid classId)
        {
            var result = await tuitionFeeService.GetClassFeeConfigAsync(classId);
            return Ok(result);
        }

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

        [HttpPut("class/{classId}/extend-due-date")]
        public async Task<IActionResult> ExtendClassInvoices(Guid classId, [FromBody] ExtendClassInvoicesDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await tuitionFeeService.ExtendClassInvoicesAsync(classId, dto);
                return Ok(new { Message = $"Đã gia hạn thành công thêm {dto.AdditionalDays} ngày cho hóa đơn." });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { Message = "Lỗi hệ thống." }); }
        }

        [HttpGet("invoices/summary")]
        public async Task<IActionResult> GetSummary([FromQuery] Guid? classId, [FromQuery] int month, [FromQuery] int year)
        {
            var result = await tuitionFeeService.GetTuitionSummaryAsync(classId, month, year);
            return Ok(result);
        }

        [HttpGet("reports/classes-overview")]
        public async Task<IActionResult> GetClassesOverview([FromQuery] int month, [FromQuery] int year)
        {
            var result = await tuitionFeeService.GetClassesOverviewAsync(month, year);
            return Ok(result);
        }

        [HttpGet("reminders")]
        public async Task<IActionResult> GetReminders([FromQuery] int month, [FromQuery] int year)
        {
            int targetMonth = month > 0 ? month : DateTime.Now.Month;
            int targetYear = year > 0 ? year : DateTime.Now.Year;
            var result = await tuitionFeeService.GetPendingInvoiceRemindersAsync(targetMonth, targetYear);
            return Ok(result);
        }

        [HttpGet("transactions/full-history")]
        public async Task<IActionResult> GetFullHistory()
        {
            var result = await tuitionFeeService.GetHistoryFullAsync();
            return Ok(result);
        }

        [HttpGet("dashboard/overview")]
        public async Task<IActionResult> GetDashboardOverview([FromQuery] int month, [FromQuery] int year)
        {
            var targetMonth = month > 0 ? month : DateTime.Now.Month;
            var targetYear = year > 0 ? year : DateTime.Now.Year;
            var result = await tuitionFeeService.GetDashboardDataAsync(targetMonth, targetYear);
            return Ok(result);
        }

        [HttpGet("class/{classId}/transactions")]
        public async Task<IActionResult> GetClassTransactions(Guid classId)
        {
            var result = await tuitionFeeService.GetTransactionsByClassAsync(classId);
            return Ok(result);
        }

        [HttpGet("class/{classId}/transactions-period")]
        public async Task<IActionResult> GetClassTransactions(Guid classId, [FromQuery] int month, [FromQuery] int year)
        {
            int targetMonth = month > 0 ? month : DateTime.Now.Month;
            int targetYear = year > 0 ? year : DateTime.Now.Year;
            var result = await tuitionFeeService.GetClassTransactionsByPeriodAsync(classId, targetMonth, targetYear);
            return Ok(result);
        }

        [HttpGet("student/myTuitions")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentTuitions([FromQuery] TuitionFilter filter)
        {
            try
            {
                var result = await _tuitionService.GetMyTuitionAsync(filter);
                if (result == null) throw new Exception("Bạn chưa có khoản học phí nào.");
                return Ok(new { Message = "Lấy danh sách học phí thành công", Data = result });
            }
            catch (Exception ex) { return BadRequest(new { Error = ex.Message }); }
        }

        [HttpGet("student/{invoiceId}/detail")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentTuitionDetail(Guid invoiceId)
        {
            try
            {
                var result = await _tuitionService.GetTuitionInvoiceDetailAsync(invoiceId);
                if (result == null) throw new Exception("Không tìm thấy hóa đơn");
                return Ok(new { Message = "Lấy chi tiết hóa đơn thành công", Data = result });
            }
            catch (Exception ex) { return BadRequest(new { Error = ex.Message }); }
        }

        [HttpGet("{invoiceId}/paymentQr")]
        public async Task<IActionResult> GetPaymentQr(Guid invoiceId)
        {
            try
            {
                var result = await _tuitionService.GetPaymentQrCodeAsync(invoiceId);
                return Ok(new { Message = "Tạo mã QR thanh toán thành công", Data = result });
            }
            catch (Exception ex) { return BadRequest(new { Error = ex.Message }); }
        }

        [HttpPost("{invoiceId}/proof")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UploadPaymentProof(Guid invoiceId, [FromForm] ProofUploadDto request)
        {
            try
            {
                if (request.ProofImage == null || request.ProofImage.Length == 0)
                    return BadRequest(new { Message = "Vui lòng chọn ảnh minh chứng giao dịch." });

                await _tuitionService.UploadPaymentProofAsync(invoiceId, request);
                return Ok(new { Message = "Nộp minh chứng thành công. Vui lòng chờ giáo viên xác nhận!" });
            }
            catch (Exception ex) { return BadRequest(new { Error = ex.Message }); }
        }

        [HttpGet("student/myTransactions")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentTransactions([FromQuery] Guid? classId)
        {
            try
            {
                var result = await _tuitionService.GetMyTransactionsAsync(classId);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { Error = ex.Message }); }
        }

        [HttpGet("student/myTransactions/{transactionId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentTransactionDetail(Guid transactionId)
        {
            try
            {
                var result = await _tuitionService.GetTransactionByIdAsync(transactionId);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { Error = ex.Message }); }
        }
    }
}