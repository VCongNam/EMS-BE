using EMS.Application.Features.Tuition.DTOs;
using EMS.Application.Features.Tuition.Services;
using EMS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Teacher")]
    public class TuitionController : ControllerBase
    {
        private readonly ITuitionService _tuitionService;
        public TuitionController(ITuitionService tuitionService)
        {
            _tuitionService = tuitionService;
        }

        [HttpPost("{transactionId}/review")]
        public async Task<IActionResult> ReviewTransaction(Guid transactionId, [FromBody] ReviewTransactionDto request)
        { 
            try
            {
                var result = await _tuitionService.ReviewTransactionAsync(transactionId, request);

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
