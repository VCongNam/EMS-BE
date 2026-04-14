using EMS.Domain.Entities;
using EMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EMS.Domain.Interfaces;

namespace EMS.Infrastructure.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly ApplicationDbContext context;

        public FeedbackRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task AddAsync(SystemFeedback fb)
        {
            await context.Set<SystemFeedback>().AddAsync(fb);
            await context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SystemFeedback>> GetAllAsync(string? t, string? s)
        {
            var q = context.Set<SystemFeedback>().Include(f => f.Sender).AsNoTracking();
            if (!string.IsNullOrEmpty(t)) { q = q.Where(f => f.Type == t); }
            if (!string.IsNullOrEmpty(s)) { q = q.Where(f => f.Status == s); }
            return await q.OrderByDescending(f => f.CreatedAt).ToListAsync();
        }

        public async Task<SystemFeedback?> GetByIdAsync(Guid id)
        {
            return await context.Set<SystemFeedback>().Include(f => f.Sender)
                .FirstOrDefaultAsync(f => f.FeedbackId == id);
        }

        public async Task UpdateAsync(SystemFeedback fb)
        {
            context.Set<SystemFeedback>().Update(fb);
            await context.SaveChangesAsync();
        }
        public async Task<IEnumerable<SystemFeedback>> GetBySenderIdAsync(Guid senderId)
        {
            return await context.Set<SystemFeedback>()
                .Where(f => f.SenderId == senderId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }
    }

}
