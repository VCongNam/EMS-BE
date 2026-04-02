using EMS.Application.Features.Financials.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FinancialController : ControllerBase
    {
        private readonly IFinancialService financialService;

        public FinancialController(IFinancialService financialService)
        {
            this.financialService = financialService;
        }

        [HttpGet("report")]
        public async Task<IActionResult> GetOverallPaymentReport()
        {
            try
            {
                var report = await financialService.GetOverallPaymentReportAsync();
                return Ok(report);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("class/{classId}/detail")]
        public async Task<IActionResult> GetClassFinancialDetail(Guid classId)
        {
            try
            {
                var classDetail = await financialService.GetClassFinancialDetailAsync(classId);
                return Ok(classDetail);
            }
            catch (Exception ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }
    }
}
