using Microsoft.AspNetCore.SignalR;

namespace EMS.API.BackgroundServices
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
    }
}
