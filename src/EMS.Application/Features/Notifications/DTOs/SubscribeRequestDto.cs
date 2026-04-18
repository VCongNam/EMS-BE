using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Notifications.DTOs
{
    public class SubscribeRequestDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
    }
    public class UnsubscribeRequestDto
    {
        public string Endpoint { get; set; } = string.Empty;
    }
}
