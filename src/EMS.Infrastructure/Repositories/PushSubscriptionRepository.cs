using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Repositories
{
    public class PushSubscriptionRepository : IPushSubscriptionRepository
    {
        private readonly ApplicationDbContext _context;
        public PushSubscriptionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PushSubscription subscription)
        {
            await _context.PushSubscriptions.AddAsync(subscription);
        }

        public async Task DeleteByEndpointAsync(string endpoint)
        {
            var subscription = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == endpoint);

            if (subscription != null)
            {
                _context.PushSubscriptions.Remove(subscription);
            }
        }

        public async Task<PushSubscription?> GetByEndpointAsync(string endpoint)
        {
            return await _context.PushSubscriptions
                 .FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        }

        public async Task<List<PushSubscription>> GetSubscriptionsByAccountIdAsync(Guid accountId)
        {
            return await _context.PushSubscriptions
                .Where(s => s.AccountId == accountId)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PushSubscription subscription)
        {
            _context.PushSubscriptions.Update(subscription);
            await Task.CompletedTask;
        }
    }
}
