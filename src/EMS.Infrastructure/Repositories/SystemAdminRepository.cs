using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class SystemAdminRepository : ISystemAdminRepository
    {
        private readonly ApplicationDbContext context;

        public SystemAdminRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<int> CountAccountsByRoleAsync(string roleName)
        {
            return await context.Accounts
                .Include(a => a.Role)
                .CountAsync(a => a.Role.RoleName == roleName && a.IsDeleted != true);
        }

        public async Task<int> CountActiveClassesAsync()
        {
            return await context.Classes
                .CountAsync(c => c.Status == "Ongoing" && c.IsDeleted != true);
        }

        public async Task<int> CountNewRegistrationsThisMonthAsync()
        {
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            return await context.Accounts
                .CountAsync(a => a.CreatedAt >= startOfMonth && a.IsDeleted != true);
        }

        public async Task<IEnumerable<Account>> GetAllAccountsAsync(string? role, string? status)
        {
            var query = context.Accounts
                .Include(a => a.Role)
                .Where(a => a.IsDeleted != true)
                .AsQueryable();

            if (!string.IsNullOrEmpty(role))
                query = query.Where(a => a.Role.RoleName == role);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        }

        public async Task<Account?> GetAccountByIdAsync(Guid accountId)
        {
            return await context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.AccountId == accountId && a.IsDeleted != true);
        }

        public async Task UpdateAccountAsync(Account account)
        {
            context.Accounts.Update(account);
            await context.SaveChangesAsync();
        }

        public async Task<int> CountClassesByTeacherAsync(Guid teacherId)
        {
            return await context.Classes
                .CountAsync(c => c.TeacherId == teacherId && c.IsDeleted != true);
        }

        public async Task<IEnumerable<SystemLog>> GetRecentSystemLogsAsync(int limit)
        {
            return await context.SystemLogs
                .AsNoTracking()
                .Include(log => log.Account)
                    .ThenInclude(acc => acc.Role)
                .OrderByDescending(log => log.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}
