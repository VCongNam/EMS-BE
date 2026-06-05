using EMS.Application.Common.Settings;
using EMS.Application.Features.Notifications.DTOs;
using EMS.Application.Features.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly VapidSettings _vapidSettings;
        public NotificationController(
            INotificationService notificationService,
            IOptions<VapidSettings> vapidSettings)
        { 
            _notificationService = notificationService;
            _vapidSettings = vapidSettings.Value;
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
            await _notificationService.MarkAsReadAsync(notificationId);
            return NoContent();
        }

        [HttpPatch("mark-all-as-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync();
            return NoContent();
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var count = await _notificationService.CountUnreadAsync();

            return Ok(new { count });
        }

        //Web Push
        [HttpGet("public-key")]
        [AllowAnonymous]
        public IActionResult GetPublicKey()
        {
            return Ok(new { publicKey = _vapidSettings.PublicKey });
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto request)
        {
            try
            {
                await _notificationService.SubscribeAsync(request);
                return Ok(new { Message = "Đã đăng ký nhận thông báo đẩy thành công!" });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequestDto request)
        {
            await _notificationService.UnsubscribeAsync(request.Endpoint);
            return NoContent();
        }
    }
}
