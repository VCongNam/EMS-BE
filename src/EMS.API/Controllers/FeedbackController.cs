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
        private readonly IFeedbackService feedbackService;
        public FeedbackController(IFeedbackService feedbackService) { 
            this.feedbackService = feedbackService; 
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFeedbackDto dto)
        {
            var uid = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
            await feedbackService.CreateFeedbackAsync(uid, dto);
            return Ok(new { Message = "Gửi phản hồi thành công!" });
        }


        [HttpGet("history")]
        public async Task<IActionResult> GetMyFeedbackHistory()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

                var result = await feedbackService.GetTeacherHistoryAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

    }
}
