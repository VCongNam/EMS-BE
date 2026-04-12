using EMS.Application.Features.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        public NotificationController(INotificationService notificationService)
        { 
            _notificationService = notificationService;
        }

        [HttpGet("Notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            try
            {
                var result = await _notificationService.GetNotificationsAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPatch("mark-as-read/{notificationId}")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            await _notificationService.MaskAsReadAsync(notificationId);
            return NoContent();
        }

        [HttpPatch("mark-all-as-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.MaskAllAsReadAsync();
            return NoContent();
        }
    }
}
