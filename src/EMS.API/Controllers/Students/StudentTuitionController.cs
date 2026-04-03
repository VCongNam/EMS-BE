using EMS.Application.Features.Students.DTOs;
using EMS.Application.Features.Students.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers.Students
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentTuitionController : ControllerBase
    {
        private readonly IStudentTuitionService _tuitionService;
        public StudentTuitionController(IStudentTuitionService tuitionService)
        {
            _tuitionService = tuitionService;
        }

        [HttpGet("tuitions")]
        public async Task<IActionResult> GetTuitions([FromQuery] TuitionFilter filter)
        {
            try
            {
                var result = await _tuitionService.GetMyTuitionAsync(filter);
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

        [HttpGet("{invoiceId}")]
        public async Task<IActionResult> GetTuitionDetail(Guid invoiceId)
        {
            try
            {
                var result = await _tuitionService.GetTuitionInvoiceDetailAsync(invoiceId);
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
    }
}
