using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetMyNotificationsAsync(Guid accountId, Guid? studentId);
    }
}
