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

            var teacherId = currentUserService.UserId;

            var result = await tuitionFeeService.GetTuitionFeeConfigsAsync(teacherId);



            return Ok(result);

        }



        /// <summary>

        /// UC1: Cập nhật đơn giá và hình thức thu (Thu trước/Thu sau) cho 1 lớp.

        /// </summary>

        [HttpPut("class/{classId}/fee")]

        public async Task<IActionResult> UpdateFee(Guid classId, UpdateTuitionFeeDto dto)

        {

            var teacherId = currentUserService.UserId;

            await tuitionFeeService.UpdateTuitionFeeAsync(classId, dto, teacherId);



            return Ok(new { Message = "Cập nhật cấu hình học phí thành công." });

        }



        /// <summary>

        /// UC5 & UC2: Phát hành hóa đơn hàng loạt cho cả lớp.

        /// </summary>

        [HttpPost("class/{classId}/generate-invoices")]

        public async Task<IActionResult> Generate(Guid classId, GenerateInvoiceDto dto)

        {

            var teacherId = currentUserService.UserId;

            await tuitionFeeService.GenerateInvoicesForClassAsync(classId, dto, teacherId);



            return Ok(new { Message = "Đã phát hành hóa đơn cho kỳ này thành công." });

        }



        /// <summary>

        /// Tính năng Cấn trừ: Chốt sổ cuối tháng để tính tiền hoàn lại cho học sinh.

        /// </summary>

        [HttpPost("class/{classId}/reconcile")]

        public async Task<IActionResult> ReconcilePrepaid(Guid classId, [FromQuery] int month, [FromQuery] int year)

        {

            var teacherId = currentUserService.UserId;

            await tuitionFeeService.ReconcilePrepaidClassAsync(classId, month, year, teacherId);



            return Ok(new { Message = "Đã chốt sổ và cộng tiền cấn trừ cho tháng sau." });

        }



        /// <summary>

        /// UC6: Lấy danh sách các giao dịch (hóa đơn chuyển khoản) đang chờ xử lý của các lớp giáo viên đang dạy.

        /// </summary>

        [HttpGet("transactions/pending")]

        public async Task<IActionResult> GetPending()

        {

            var teacherId = currentUserService.UserId;

            var result = await tuitionFeeService.GetPendingTransactionsAsync(teacherId);



            return Ok(result);

        }



        /// <summary>

        /// UC7: Phê duyệt hoặc Từ chối một giao dịch chuyển khoản.

        /// </summary>

        [HttpPost("transaction/{id}/review")]

        public async Task<IActionResult> Review(Guid id, [FromBody] ReviewTransactionDto dto)

        {

            await tuitionFeeService.ReviewTransactionAsync(id, dto.IsApproved, currentUserService.UserId, dto.Note);



            return Ok(new { Message = "Đã xử lý giao dịch thành công." });

        }



        /// <summary>

        /// UC4: Lấy chi tiết báo cáo công nợ của từng học sinh trong một lớp.

        /// </summary>

        [HttpGet("class/{classId}/detail")]

        public async Task<IActionResult> GetDetail(Guid classId, [FromQuery] int month, [FromQuery] int year)

        {

            var teacherId = currentUserService.UserId;

            var result = await tuitionFeeService.GetClassFinancialDetailAsync(classId, month, year, teacherId);



            return Ok(result);

        }



        /// <summary>

        /// UC3: Lấy báo cáo doanh thu tổng quan của riêng giáo viên đó.

        /// </summary>

        [HttpGet("report/summary")]

        public async Task<IActionResult> GetReportSummary()

        {

            var teacherId = currentUserService.UserId;

            var result = await tuitionFeeService.GetOverallReportAsync(teacherId);



            return Ok(result);

        }



        [HttpGet("report/class-summaries")]

        public async Task<IActionResult> GetClassFinancialSummaries()

        {

            var teacherId = currentUserService.UserId;

            var result = await tuitionFeeService.GetClassFinancialSummariesAsync(teacherId);

            return Ok(result);

        }



        [HttpGet("report/dashboard-analytics")]

        public async Task<IActionResult> GetDashboardAnalytics()

        {

            var teacherId = currentUserService.UserId;

            var result = await tuitionFeeService.GetDashboardAnalyticsAsync(teacherId);

            return Ok(result);

        }



        [HttpPut("invoice/{invoiceId}/extend-due-date")]

        public async Task<IActionResult> ExtendDueDate(Guid invoiceId, [FromBody] ExtendInvoiceDto dto)

        {

            var teacherId = currentUserService.UserId;

            await tuitionFeeService.ExtendInvoiceDueDateAsync(invoiceId, dto.AdditionalDays, teacherId);

            return Ok(new { Message = $"Đã gia hạn hóa đơn thêm {dto.AdditionalDays} ngày." });

        }

        /// <summary>

        /// Gia hạn hàng loạt tất cả hóa đơn đang nợ của một lớp trong một kỳ cụ thể

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