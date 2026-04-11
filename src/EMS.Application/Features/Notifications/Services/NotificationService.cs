using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Notifications.DTOs;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Notifications.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ICurrentUserService _currentUser;

        public NotificationService(INotificationRepository notificationRepository, ICurrentUserService currentUser)
        {
            _notificationRepository = notificationRepository;
            _currentUser = currentUser;
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync()
        {
            Guid accountId = _currentUser.UserId;
            Guid? studentId = _currentUser.StudentId;
            var notification = await _notificationRepository.GetMyNotificationsAsync(accountId,studentId);
            if (notification == null)
            {
                throw new Exception("Bạn chưa có thông báo nào");
            }
            return notification.Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Content = n.Content,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                Type = n.Type,
                ActionUrl = n.ActionUrl,
                StudentId = n.StudentId,
            }).ToList();
        }
    }
}
