using DocumentFormat.OpenXml.Office2016.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using EMS.Application.Common.DTOs;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Notifications.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EMS.Application.Features.Notifications.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IWebPushService _webPushService;
        private readonly IPushSubscriptionRepository _pushRepo; // Repo xử lý bảng PushSubscriptions
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(INotificationRepository notificationRepository,
            ICurrentUserService currentUser,
            IWebPushService webPushService,
            IPushSubscriptionRepository pushSubscriptionRepository,
             ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _currentUser = currentUser;
            _webPushService = webPushService;
            _pushRepo = pushSubscriptionRepository;
            _logger = logger;
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

        public async Task MarkAllAsReadAsync()
        {
            Guid accountId = _currentUser.UserId;
            Guid? studentId = _currentUser.StudentId;

            if (accountId == Guid.Empty) throw new UnauthorizedAccessException();
            await _notificationRepository.MarkAllAsReadAsync(accountId, studentId);
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            Guid accountId = _currentUser.UserId;
            Guid? studentId = _currentUser.StudentId;
            if (accountId == Guid.Empty) throw new UnauthorizedAccessException();
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
                CreatedAt = DateTime.UtcNow,
                Type = type
            };

            await _notificationRepository.AddAsync(notification);


            //Web Push
            int unreadCount = await _notificationRepository.CountUnreadAsync(targetAccountId, studentId);

            var payloadObj = new WebPushPayload
            {
                Title = title,
                Body = content,
                Url = actionUrl,
                Data = new
                {
                    notificationId = notification.NotificationId,
                    studentId = studentId,
                    type = type,
                    badgeCount = unreadCount
                }
            };
            string payloadJson = JsonSerializer.Serialize(payloadObj);

            var subscriptions = await _pushRepo.GetSubscriptionsByAccountIdAsync(targetAccountId);

            foreach (var sub in subscriptions)
            {
                try
                {
                    await _webPushService.SendNotificationAsync(sub.Endpoint, sub.P256dh, sub.Auth, payloadJson);
                }
                catch (SubscriptionExpiredException ex)
                {
                    // Tự động xóa thiết bị đã gỡ PWA khỏi DB
                    await _pushRepo.DeleteByEndpointAsync(ex.ExpiredEndpoint);
                    await _pushRepo.SaveChangesAsync();
                    _logger.LogInformation($"Đã xóa subscription rác khỏi DB: {ex.ExpiredEndpoint}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi gửi Push tới endpoint {sub.Endpoint}");
                }
            }
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

            //Web push
            var uniqueAccountIds = targets.Select(t => t.AccId).Distinct();

            foreach (var accId in uniqueAccountIds)
            {
                var relevantStudentIds = targets.Where(t => t.AccId == accId).Select(t => t.StdId).ToList();

                var payloadObj = new WebPushPayload
                {
                    Title = title,
                    Body = content,
                    Url = actionUrl,
                    Data = new
                    {
                        type = type,
                        relevantStudentIds = relevantStudentIds
                    }
                };
                string payloadJson = JsonSerializer.Serialize(payloadObj);

                var subscriptions = await _pushRepo.GetSubscriptionsByAccountIdAsync(accId);

                foreach (var sub in subscriptions)
                {
                    try
                    {
                        await _webPushService.SendNotificationAsync(sub.Endpoint, sub.P256dh, sub.Auth, payloadJson);
                    }
                    catch (SubscriptionExpiredException ex)
                    {
                        await _pushRepo.DeleteByEndpointAsync(ex.ExpiredEndpoint);
                        await _pushRepo.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi gửi Bulk Push tới endpoint {sub.Endpoint}");
                    }
                }
            }
        }

        public async Task SubscribeAsync(SubscribeRequestDto request)
        {
            var accountId = _currentUser.UserId;
            if (accountId == Guid.Empty) throw new UnauthorizedAccessException();

            var existing = await _pushRepo.GetByEndpointAsync(request.Endpoint);

            if (existing != null)
            {
                existing.P256dh = request.P256dh;
                existing.Auth = request.Auth;
                existing.DeviceName = request.DeviceName;
                existing.AccountId = accountId;

                await _pushRepo.UpdateAsync(existing);
            }
            else
            {
                var newSub = new PushSubscription
                {
                    SubscriptionId = Guid.NewGuid(),
                    AccountId = accountId,
                    Endpoint = request.Endpoint,
                    P256dh = request.P256dh,
                    Auth = request.Auth,
                    DeviceName = request.DeviceName,
                    CreatedAt = DateTime.UtcNow
                };
                await _pushRepo.AddAsync(newSub);
            }
            await _pushRepo.SaveChangesAsync();
        }

        public async Task UnsubscribeAsync(string endpoint)
        {
            await _pushRepo.DeleteByEndpointAsync(endpoint);
            await _pushRepo.SaveChangesAsync();
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
