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
    public class TuitionRepository : ITuitionRepository
    {
        private readonly ApplicationDbContext _context;
        public TuitionRepository(ApplicationDbContext context) 
        {
            _context = context;
        }

        public async Task<(List<(Invoice Invoice, Transaction? LatestTransaction)> Items, int TotalCount)> GetStudentInvoicesAsync(Guid studentId, int page, int size, Guid? classId)
        {
            var query = _context.Invoices
                .Include(i => i.Class)
                .Where(i => i.StudentId == studentId)
                .AsNoTracking();
            if (classId.HasValue)
            {
                query = query.Where(i => i.ClassId == classId.Value);
            }

            int totalCount = await query.CountAsync();
            var dbResult = await query
                .OrderBy(i => i.DueDate)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(i => new
                {
                    Invoice = i,
                    LatestTransaction = _context.Transactions
                        .Where(t => t.InvoiceId == i.InvoiceId)
                        .OrderByDescending(t => t.CreatedAt)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var items = dbResult.Select(x => (x.Invoice, x.LatestTransaction)).ToList();
            return (items, totalCount);
        }

       
    }
}
