using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Common.Interfaces
{
    public interface IWebPushService
    {
        Task SendNotificationAsync(string endpoint, string p256dh, string auth, string payload);
    }
}
