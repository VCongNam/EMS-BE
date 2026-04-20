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
        private readonly ICurrentUserService currentUserService;
        private readonly IStudentTuitionService _tuitionService;

        public TuitionFeeController(ITuitionFeeService tuitionFeeService, ICurrentUserService currentUserService, IStudentTuitionService tuitionService)
        {
            this.tuitionFeeService = tuitionFeeService;
            this.currentUserService = currentUserService;
            _tuitionService = tuitionService;
        }

        [HttpPost("class/{classId}/reconcile")]
        [Authorize(Roles = "Teacher")]
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


        [HttpGet("transactions/pending")]
        [Authorize(Roles = "Teacher")]
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
        [Authorize(Roles = "Teacher")]
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

        [HttpGet("invoices/report")]
        public async Task<IActionResult> GetInvoicesReport([FromQuery] Guid? classId, [FromQuery] int month, [FromQuery] int year)
        {
            try
            {
                var result = await tuitionFeeService.GetInvoicesListAsync(classId, month, year);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi tải danh sách hóa đơn." });
            }
        }



        [HttpGet("configs")]
        public async Task<IActionResult> GetFeeConfigs()
        {
            try
            {
                var result = await tuitionFeeService.GetClassFeeConfigsAsync();
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception) { return StatusCode(500, new { Message = "Lỗi hệ thống khi tải cấu hình." }); }
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
                return Ok(new { Message = $"Đã gia hạn thành công thêm {dto.AdditionalDays} ngày cho toàn bộ hóa đơn đang nợ của lớp." });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { Message = "Lỗi hệ thống." }); }
        }


        [HttpGet("invoices/summary")]
        public async Task<IActionResult> GetSummary([FromQuery] Guid? classId, [FromQuery] int month, [FromQuery] int year)
        {
            try
            {
                var result = await tuitionFeeService.GetTuitionSummaryAsync(classId, month, year);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi tính toán doanh thu tổng hợp kỳ này." });
            }
        }

        [HttpGet("reports/classes-overview")]
        public async Task<IActionResult> GetClassesOverview([FromQuery] int month, [FromQuery] int year)
        {
            try
            {
                var result = await tuitionFeeService.GetClassesOverviewAsync(month, year);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi tải danh sách báo cáo tổng quan lớp học." });
            }
        }


        [HttpPost("class/{classId}/generate-invoices")]
        public async Task<IActionResult> Generate(Guid classId, [FromBody] GenerateInvoiceDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var teacherId = currentUserService.UserId;
                await tuitionFeeService.GenerateInvoicesForClassAsync(classId, dto, teacherId);
                return Ok(new { Message = "Đã phát hành hóa đơn cho kỳ học thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }

        [HttpGet("reminders")]
        public async Task<IActionResult> GetReminders([FromQuery] int month, [FromQuery] int year)
        {
            try
            {
                // Mặc định lấy tháng hiện tại nếu FE không truyền
                int targetMonth = month > 0 ? month : DateTime.Now.Month;
                int targetYear = year > 0 ? year : DateTime.Now.Year;

                var result = await tuitionFeeService.GetPendingInvoiceRemindersAsync(targetMonth, targetYear);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi tải danh sách nhắc nhở." });
            }
        }


        [HttpGet("transactions/full-history")]
        public async Task<IActionResult> GetFullHistory()
        {
            try
            {
                var result = await tuitionFeeService.GetHistoryFullAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi tải lịch sử: " + ex.Message });
            }
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
            try
            {
                var result = await tuitionFeeService.GetTransactionsByClassAsync(classId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy lịch sử giao dịch của lớp: " + ex.Message });
            }
        }


        [HttpGet("class/{classId}/transactions-period")]
        public async Task<IActionResult> GetClassTransactions(Guid classId, [FromQuery] int month, [FromQuery] int year)
        {
            int targetMonth = month > 0 ? month : DateTime.Now.Month;
            int targetYear = year > 0 ? year : DateTime.Now.Year;

            try
            {
                var result = await tuitionFeeService.GetClassTransactionsByPeriodAsync(classId, targetMonth, targetYear);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lọc giao dịch theo kỳ: " + ex.Message });
            }
        }


        [HttpGet("student/myTuitions")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentTuitions([FromQuery] TuitionFilter filter)
        {
            try
            {
                var result = await _tuitionService.GetMyTuitionAsync(filter);
                if (result == null) throw new Exception("Bạn chưa có khoản học phí nào.");
                return Ok(new
                {
                    Message = "Lấy danh sách học phí thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("student/{invoiceId}/detail")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentTuitionDetail(Guid invoiceId)
        {
            try
            {
                var result = await _tuitionService.GetTuitionInvoiceDetailAsync(invoiceId);
                if (result == null) throw new Exception("Không tìm thấy hóa đơn");
                return Ok(new
                {
                    Message = "Lấy chi tiết hóa đơn thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("{invoiceId}/paymentQr")]
        public async Task<IActionResult> GetPaymentQr(Guid invoiceId)
        {
            try
            {
                var result = await _tuitionService.GetPaymentQrCodeAsync(invoiceId);
                return Ok(new
                {
                    Message = "Tạo mã QR thanh toán thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{invoiceId}/proof")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UploadPaymentProof(Guid invoiceId, [FromForm] ProofUploadDto request)
        {
            try
            {
                if (request.ProofImage == null || request.ProofImage.Length == 0)
                {
                    return BadRequest(new { Message = "Vui lòng chọn ảnh minh chứng giao dịch." });
                }

                await _tuitionService.UploadPaymentProofAsync(invoiceId, request);

                return Ok(new
                {
                    Message = "Nộp minh chứng thành công. Vui lòng chờ giáo viên xác nhận!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
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
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
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
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

    }
}