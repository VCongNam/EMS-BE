using EMS.Application.Features.TuitionFees.Dtos;
using EMS.Application.Features.TuitionFees.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager")] // Chỉ Admin/Quản lý được thiết lập học phí
    public class TuitionFeeController : ControllerBase
    {
        private readonly ITuitionFeeService tuitionFeeService;

        public TuitionFeeController(ITuitionFeeService tuitionFeeService)
        {
            this.tuitionFeeService = tuitionFeeService;
        }

        [HttpPut("class/{classId}/fee")]
        public async Task<IActionResult> UpdateTuitionFee(Guid classId, [FromBody] UpdateTuitionFeeDto request)
        {
            try
            {
                await tuitionFeeService.UpdateTuitionFeeAsync(classId, request);
                return Ok(new { Message = "Đã cập nhật học phí của lớp thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("class/{classId}/deadline")]
        public async Task<IActionResult> UpdateTuitionDeadline(Guid classId, [FromBody] UpdateTuitionFeeDeadlineDto request)
        {
            try
            {
                await tuitionFeeService.UpdateTuitionDeadlineAsync(classId, request);
                return Ok(new { Message = "Đã cập nhật hạn nộp học phí thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{transactionId}/review")]
        public async Task<IActionResult> ReviewTransaction(Guid transactionId, [FromBody] ReviewTransactionDto request)
        {
            try
            {
                var result = await tuitionFeeService.ReviewTransactionAsync(transactionId, request);

                if (result)
                    return Ok(new { Message = request.IsApproved ? "Đã duyệt học phí." : " Đã từ chối minh chứng." });

                return BadRequest(new { Message = "Có lỗi xảy ra trong quá trình xử lý." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
