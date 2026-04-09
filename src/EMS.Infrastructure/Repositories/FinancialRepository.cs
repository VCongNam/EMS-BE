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
    public class FinancialRepository : IFinancialRepository
    {
        private readonly ApplicationDbContext context;

        public FinancialRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<Class?> GetClassInfoAsync(Guid classId)
        {
            return await context.Classes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.IsDeleted != true);
        }

        public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
        {
            return await context.Invoices
                .AsNoTracking()
                .Where(i => i.IsDeleted != true)
                .Include(i => i.Transactions)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByClassIdAsync(Guid classId)
        {
            return await context.Invoices
                .AsNoTracking()
                .Where(i => i.ClassId == classId && i.IsDeleted != true)
                .Include(i => i.Student)
                    .ThenInclude(s => s.Account)
                .Include(i => i.Transactions)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetRecentTransactionsAsync(int count)
        {
            return await context.Transactions
                .AsNoTracking()
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Student)
                        .ThenInclude(s => s.Account)
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Class)
                .OrderByDescending(t => t.PaidDate)
                .Take(count)
                .ToListAsync();
        }
    }
}
