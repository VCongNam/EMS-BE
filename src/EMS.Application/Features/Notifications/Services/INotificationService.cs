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
        Task MarkAsReadAsync(Guid notificationId);
        Task MarkAllAsReadAsync();
        Task<int> CountUnreadAsync();
        Task SendNotificationAsync(Guid targetAccountId, Guid? studentId, string title, string content, string actionUrl, string type);
        Task SendBulkNotificationWithStudentAsync(List<(Guid AccId, Guid? StdId)> targets, string title, string content, string actionUrl, string type);
        Task<Guid?> GetAccountIdByStudentIdAsync(Guid studentId);
        Task<List<(Guid AccId, Guid? StdId)>> GetStudentTargetsAsync(Guid classId);
        Task<List<Guid>> GetTutorTargetsAsync(Guid classId);
        Task<List<(Guid AccId, Guid? StdId)>> GetAllClassTargetsAsync(Guid classId);
        Task<(Guid taAccountId, string className)> GetTAAccountInfoByClassTaidAsync(Guid classTAID);
        Task SubscribeAsync(SubscribeRequestDto request);
        Task UnsubscribeAsync(string endpoint);
    }
}
