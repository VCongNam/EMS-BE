using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;
        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Notification>> GetMyNotificationsAsync(Guid accountId, Guid? studentId)
        {
            var query = _context.Notifications.Where(n => n.AccountId == accountId);
            if (studentId.HasValue)
            {
                query = query.Where(n => n.StudentId == studentId.Value);
            }
            return await query.OrderByDescending(n => n.CreatedAt)
                        .Take(20)
                        .ToListAsync();
        }

        public async Task MarkAllAsReadAsync(Guid accountId, Guid? studentId)
        {
            var query = _context.Notifications
                .Where(n => n.AccountId == accountId && n.IsRead == false);
            if (studentId.HasValue)
            {
                query = query.Where(n => n.StudentId == null || n.StudentId == studentId.Value);

                var unreadNotifications = await query.ToListAsync();
                foreach(var n in unreadNotifications)
                {
                    n.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAsReadAsync(Guid notificationId, Guid accountId, Guid? studentId)
        {
            var query = _context.Notifications
                    .Where(n => n.NotificationId == notificationId && n.AccountId == accountId);

            if (studentId.HasValue)
            {
                query = query.Where(n => n.StudentId == studentId);
            }
            var notification = await query.FirstOrDefaultAsync();
            if (notification != null && notification.IsRead == false)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> CountUnreadAsync(Guid accountId, Guid? studentId)
        {
            var query = _context.Notifications
                .Where(n => n.AccountId == accountId && n.IsRead == false);

            if (studentId.HasValue)
            {
                query = query.Where(n => n.StudentId == studentId);
            }

            return await query.CountAsync();
        }

        public async Task AddAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<Notification> notifications)
        {
            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }
    }
}
