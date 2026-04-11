using EMS.Application.Features.Notifications.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Notifications.Services
{
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetNotificationsAsync();
    }
}
