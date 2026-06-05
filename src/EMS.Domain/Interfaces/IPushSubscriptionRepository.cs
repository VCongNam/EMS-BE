using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface IPushSubscriptionRepository
    {
        Task<List<PushSubscription>> GetSubscriptionsByAccountIdAsync(Guid accountId); 
        Task<PushSubscription?> GetByEndpointAsync(string endpoint);
        Task AddAsync(PushSubscription subscription);
        Task UpdateAsync(PushSubscription subscription);
        Task DeleteByEndpointAsync(string endpoint);
        Task SaveChangesAsync();
    }
}
