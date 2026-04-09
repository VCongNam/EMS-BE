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

        // ========================================================
        // NHÓM 1: CẤU HÌNH & CÀI ĐẶT
        // ========================================================

        /// <summary>
        /// Màn hình: Cấu hình học phí.
        /// Chức năng: Lấy danh sách tất cả các lớp của giáo viên này kèm theo đơn giá, hình thức thu (trước/sau) hiện tại.
        /// </summary>
        [HttpGet("configs")]
        public async Task<IActionResult> GetConfigs()
        {
            var teacherId = currentUserService.UserId;
            var result = await tuitionFeeService.GetTuitionFeeConfigsAsync(teacherId);
            return Ok(result);
        }

        /// <summary>
        /// Màn hình: Cấu hình học phí (Popup/Modal chỉnh sửa).
        /// Chức năng: Cập nhật giá tiền, hình thức thu và số ngày hạn nộp cho 1 lớp cụ thể.
        /// </summary>
        [HttpPut("class/{classId}/fee")]
        public async Task<IActionResult> UpdateFee(Guid classId, UpdateTuitionFeeDto dto)
        {
            var teacherId = currentUserService.UserId;
            await tuitionFeeService.UpdateTuitionFeeAsync(classId, dto, teacherId);
            return Ok(new { Message = "Cập nhật cấu hình học phí thành công." });
        }

        // ========================================================
        // NHÓM 2: VẬN HÀNH HÀNG THÁNG (CHỐT SỔ & PHÁT HÀNH HÓA ĐƠN)
        // ========================================================

        /// <summary>
        /// Màn hình: Quản lý lớp học / Chốt tháng.
        /// Chức năng: Tự động tính toán số buổi học và tạo hóa đơn (đòi tiền) gửi cho toàn bộ học sinh trong lớp.
        /// </summary>
        [HttpPost("class/{classId}/generate-invoices")]
        public async Task<IActionResult> Generate(Guid classId, GenerateInvoiceDto dto)
        {
            var teacherId = currentUserService.UserId;
            await tuitionFeeService.GenerateInvoicesForClassAsync(classId, dto, teacherId);
            return Ok(new { Message = "Đã phát hành hóa đơn cho kỳ này thành công." });
        }

        /// <summary>
        /// Màn hình: Quản lý lớp học (Chỉ dùng cho lớp Thu trước).
        /// Chức năng: Quét các học sinh nghỉ có phép trong tháng, tính ra tiền thừa và cộng vào "Ví cấn trừ" để giảm giá cho tháng sau.
        /// </summary>
        [HttpPost("class/{classId}/reconcile")]
        public async Task<IActionResult> ReconcilePrepaid(Guid classId, [FromQuery] int month, [FromQuery] int year)
        {
            var teacherId = currentUserService.UserId;
            await tuitionFeeService.ReconcilePrepaidClassAsync(classId, month, year, teacherId);
            return Ok(new { Message = "Đã chốt sổ và cộng tiền cấn trừ cho tháng sau." });
        }

        // ========================================================
        // NHÓM 3: XỬ LÝ THANH TOÁN (DUYỆT BILL CHUYỂN KHOẢN)
        // ========================================================

        /// <summary>
        /// Màn hình: Danh sách chờ duyệt (Pending Transactions).
        /// Chức năng: Kéo về các ảnh chụp màn hình chuyển khoản mà học sinh nộp lên, đang chờ giáo viên kiểm tra tài khoản.
        /// </summary>
        [HttpGet("transactions/pending")]
        public async Task<IActionResult> GetPending()
        {
            var teacherId = currentUserService.UserId;
            var result = await tuitionFeeService.GetPendingTransactionsAsync(teacherId);
            return Ok(result);
        }

        /// <summary>
        /// Màn hình: Chi tiết 1 giao dịch chờ duyệt.
        /// Chức năng: Giáo viên bấm "Đồng ý" (đã nhận được tiền) hoặc "Từ chối" (ảnh mờ, sai số tiền...).
        /// </summary>
        [HttpPost("{transactionId}/review")]
        public async Task<IActionResult> ReviewTransaction(Guid transactionId, [FromBody] ReviewTransactionDto request)
        {
            try
            {
                var result = await tuitionFeeService.ReviewTransactionAsync(transactionId, request);

                if (result)
                {
                    return Ok(new { Message = request.IsApproved ? "Đã duyệt học phí thành công." : " Đã từ chối minh chứng." });
                }

                return BadRequest(new { Message = "Có lỗi xảy ra trong quá trình xử lý." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // ========================================================
        // NHÓM 4: BÁO CÁO & THỐNG KÊ (DASHBOARD)
        // ========================================================

        /// <summary>
        /// Màn hình: Dashboard Chính.
        /// Chức năng: Trả về cục Data khủng để FE vẽ biểu đồ (Tổng doanh thu, Biểu đồ doanh thu 6 tháng, Phân bổ theo lớp).
        /// </summary>
        [HttpGet("report/dashboard-analytics")]
        public async Task<IActionResult> GetDashboardAnalytics()
        {
            var teacherId = currentUserService.UserId;
            var result = await tuitionFeeService.GetDashboardAnalyticsAsync(teacherId);
            return Ok(result);
        }

        /// <summary>
        /// Màn hình: Báo cáo công nợ tổng quan.
        /// Chức năng: Trả về danh sách các lớp, mỗi lớp hiển thị Tiền kỳ vọng thu vs Tiền thực tế đã thu được (Tỷ lệ thu hồi nợ).
        /// </summary>
        [HttpGet("report/class-summaries")]
        public async Task<IActionResult> GetClassFinancialSummaries()
        {
            var teacherId = currentUserService.UserId;
            var result = await tuitionFeeService.GetClassFinancialSummariesAsync(teacherId);
            return Ok(result);
        }

        /// <summary>
        /// Màn hình: Báo cáo công nợ chi tiết của 1 lớp.
        /// Chức năng: Bấm vào 1 lớp, xem chi tiết từng học sinh A, B, C đã nộp bao nhiêu, còn nợ bao nhiêu trong tháng đó.
        /// </summary>
        [HttpGet("class/{classId}/detail")]
        public async Task<IActionResult> GetDetail(Guid classId, [FromQuery] int month, [FromQuery] int year)
        {
            var teacherId = currentUserService.UserId;
            var result = await tuitionFeeService.GetClassFinancialDetailAsync(classId, month, year, teacherId);
            return Ok(result);
        }

        /// <summary>
        /// Màn hình: Widget nhỏ gọn (Mini stats).
        /// Chức năng: Đếm nhanh số lượng hóa đơn Đã thu/Chưa thu.
        /// </summary>
        [HttpGet("report/summary")]
        public async Task<IActionResult> GetReportSummary()
        {
            var teacherId = currentUserService.UserId;
            var result = await tuitionFeeService.GetOverallReportAsync(teacherId);
            return Ok(result);
        }

        // ========================================================
        // NHÓM 5: NGOẠI LỆ (GIA HẠN THANH TOÁN)
        // ========================================================

        /// <summary>
        /// Màn hình: Chi tiết công nợ học sinh.
        /// Chức năng: Châm chước gia hạn thêm ngày nộp tiền cho 1 học sinh cụ thể (1 hóa đơn).
        /// </summary>
        [HttpPut("invoice/{invoiceId}/extend-due-date")]
        public async Task<IActionResult> ExtendDueDate(Guid invoiceId, [FromBody] ExtendInvoiceDto dto)
        {
            var teacherId = currentUserService.UserId;
            await tuitionFeeService.ExtendInvoiceDueDateAsync(invoiceId, dto.AdditionalDays, teacherId);
            return Ok(new { Message = $"Đã gia hạn hóa đơn thêm {dto.AdditionalDays} ngày." });
        }

        /// <summary>
        /// Màn hình: Quản lý lớp học.
        /// Chức năng: Gia hạn đồng loạt cho TẤT CẢ các học sinh đang còn nợ tiền trong một lớp (Ví dụ: Tết nên gia hạn cho cả lớp thêm 7 ngày).
        /// </summary>
        [HttpPut("class/{classId}/extend-due-date")]
        public async Task<IActionResult> ExtendClassDueDate(Guid classId, [FromBody] ExtendClassInvoicesDto dto)
        {
            var teacherId = currentUserService.UserId;
            await tuitionFeeService.ExtendClassInvoicesDueDateAsync(classId, dto, teacherId);
            return Ok(new { Message = $"Đã gia hạn thành công thêm {dto.AdditionalDays} ngày cho các hóa đơn đang nợ của lớp." });
        }
    }
}