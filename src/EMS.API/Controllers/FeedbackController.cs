using EMS.Application.Features.Feedbacks.Dtos;
using EMS.Application.Features.Feedbacks.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Authorize(Roles = "Teacher")]
    [Route("api/feedback")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _svc;
        public FeedbackController(IFeedbackService svc) { _svc = svc; }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFeedbackDto dto)
        {
            var uid = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
            await _svc.CreateFeedbackAsync(uid, dto);
            return Ok(new { Message = "Gửi phản hồi thành công!" });
        }
    }
}
