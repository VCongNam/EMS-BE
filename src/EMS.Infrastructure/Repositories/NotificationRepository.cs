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
    }
}
