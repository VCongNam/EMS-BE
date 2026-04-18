using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebPush;

namespace EMS.Infrastructure.Services
{
    public class WebPushService : IWebPushService
    {
        private readonly VapidDetails _vapidDetails;
        private readonly IWebPushClient _webPushClient;
        private readonly ILogger<WebPushService> _logger;

        public WebPushService(IOptions<VapidSettings> vapidSettings, ILogger<WebPushService> logger)
        {
            var settings = vapidSettings.Value;
            _vapidDetails = new VapidDetails(
                settings.Subject,
                settings.PublicKey,
                settings.PrivateKey
            );
            _webPushClient = new WebPushClient();
            _logger = logger;
        }

        public async Task SendNotificationAsync(string endpoint, string p256dh, string auth, string payload)
        {
            try
            {
                var pushSubcription = new PushSubscription(endpoint, p256dh, auth);
                var webPushClient = new WebPushClient();

                await webPushClient.SendNotificationAsync(pushSubcription, payload, _vapidDetails);

                _logger.LogInformation("Gửi Web Push thành công tới endpoint: {Endpoint}", endpoint);
            }
            catch (WebPushException ex)
            {
                var statusCode = ex.StatusCode;
                if (statusCode == HttpStatusCode.Gone || statusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Push Subscription đã hết hạn hoặc bị xóa. Endpoint: {Endpoint}", endpoint);
                    throw new SubscriptionExpiredException(endpoint, ex);
                }
                else
                {
                    _logger.LogError(ex, "Lỗi từ Push Service. StatusCode: {StatusCode}", statusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống không xác định khi gửi Web Push.");
            }
        }
    }
}
