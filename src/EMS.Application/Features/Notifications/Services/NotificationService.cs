using DocumentFormat.OpenXml.Office2016.Excel;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Notifications.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
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
        //private readonly ISignalRService _signalRService;

        public NotificationService(INotificationRepository notificationRepository, ICurrentUserService currentUser)
        {
            _notificationRepository = notificationRepository;
            _currentUser = currentUser;
            //_signalRService = signalRService;
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync()
        {
            Guid accountId = _currentUser.UserId;
            Guid? studentId = _currentUser.StudentId;
            var notification = await _notificationRepository.GetMyNotificationsAsync(accountId,studentId);
            if (notification == null)
            {
                return new List<NotificationDto>();
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

        public async Task MaskAllAsReadAsync()
        {
            Guid accountId = _currentUser.UserId;
            Guid? studentId = _currentUser.StudentId;
            await _notificationRepository.MarkAllAsReadAsync(accountId, studentId);
        }

        public async Task MaskAsReadAsync(Guid notificationId)
        {
            Guid accountId = _currentUser.UserId;
            Guid? studentId = _currentUser.StudentId;
            await _notificationRepository.MarkAsReadAsync(notificationId, accountId, studentId);
        }

        public async Task<int> CountUnreadAsync()
        {
            Guid accountId = _currentUser.UserId;
            Guid? studentId = _currentUser.StudentId;
            return await _notificationRepository.CountUnreadAsync(accountId, studentId);
        }

        public async Task SendNotificationAsync(Guid targetAccountId, Guid? studentId, string title, string content, string actionUrl, string type)
        {
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                AccountId = targetAccountId,
                StudentId = studentId,
                Title = title,
                Content = content,
                ActionUrl = actionUrl,
                IsRead = false,
                CreatedAt = DateTime.Now,
                Type = type
            };

            await _notificationRepository.AddAsync(notification);


            //SignalR
            //int unreadCount = await _notificationRepository.CountUnreadAsync(targetAccountId, studentId);

            //var notificationData = new
            //{
            //    notification.NotificationId,
            //    notification.Title,
            //    notification.Content,
            //    notification.ActionUrl,
            //    notification.StudentId,
            //    notification.CreatedAt,
            //    BadgeCount = unreadCount 
            //};

            //await _signalRService.SendNotificationToUser(targetAccountId, notificationData);
        }

        public async Task SendBulkNotificationWithStudentAsync(List<(Guid AccId, Guid? StdId)> targets, string title, string content, string actionUrl, string type)
        {
            var notifications = new List<Notification>();
            foreach (var target in targets)
            {
                notifications.Add(new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    AccountId = target.AccId,
                    StudentId = (target.StdId == Guid.Empty) ? null : target.StdId,
                    Title = title,
                    Content = content,
                    ActionUrl = actionUrl,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    Type = type
                });
            }
            await _notificationRepository.AddRangeAsync(notifications);

            //SignalR
            //var uniqueAccountIds = targets.Select(t => t.AccId).Distinct();
            //foreach (var accId in uniqueAccountIds)
            //{
            //    await _signalRService.SendNotificationToUser(accId, new
            //    {
            //        title,
            //        content,
            //        actionUrl,
            //        RelevantStudentIds = targets.Where(t => t.AccId == accId).Select(t => t.StdId)
            //    });
            //}
        }

        public async Task<Guid?> GetAccountIdByStudentIdAsync(Guid studentId)
        {
            return await _notificationRepository.GetAccountIdByStudentId(studentId);
        }

        public async Task<List<(Guid AccId, Guid? StdId)>> GetStudentTargetsAsync(Guid classId)
        {
            return await _notificationRepository.GetStudentsInClassAsync(classId);
        }

        public async Task<List<Guid>> GetTutorTargetsAsync(Guid classId)
        {
            return await _notificationRepository.GetTutorsInClassAsync(classId);
        }

        public async Task<List<(Guid AccId, Guid? StdId)>> GetAllClassTargetsAsync(Guid classId)
        {
            return await _notificationRepository.GetAllParticipantsInClassAsync(classId);
        }

        public async Task<(Guid taAccountId, string className)> GetTAAccountInfoByClassTaidAsync(Guid classTAID)
        {
            var (taAccountId, className) = await _notificationRepository.GetTAAccountInfoByClassTaidAsync(classTAID);
            return (taAccountId, className);
        }
    }
}
