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
    public class TuitionFeeRepository : ITuitionFeeRepository
    {
        private readonly ApplicationDbContext context;

        public TuitionFeeRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<Class?> GetClassByIdAsync(Guid classId)
        {
            return await context.Classes
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.IsDeleted != true);
        }

        public async Task UpdateClassAsync(Class classEntity)
        {
            context.Classes.Update(classEntity);
            await context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByClassAndPeriodAsync(Guid classId, int month, int year)
        {
            return await context.Invoices
                .Where(i => i.ClassId == classId && i.PeriodMonth == month && i.PeriodYear == year && i.IsDeleted != true)
                .ToListAsync();
        }

        public async Task UpdateInvoicesAsync(IEnumerable<Invoice> invoices)
        {
            context.Invoices.UpdateRange(invoices);
            await context.SaveChangesAsync();
        }
    }
}
