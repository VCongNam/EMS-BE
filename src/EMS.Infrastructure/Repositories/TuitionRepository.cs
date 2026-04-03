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

        public async Task<(Invoice? Invoice, Transaction? LatestTransaction, List<Attendance> Attendances)> GetInvoiceDetailAsync(Guid invoiceId, Guid studentId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Class)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.StudentId == studentId);
            if (invoice == null) return (null, null, new List<Attendance>());
            var latestTransaction = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.InvoiceId == invoiceId)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            var attendances = await _context.Attendances
                .Include(a => a.Session)
                .AsNoTracking()
                .Where(a => a.InvoiceId == invoiceId && a.StudentId == studentId)
                .OrderBy(a => a.Session.Date)
                .ToListAsync();
            return (invoice, latestTransaction, attendances);
        }

        public async Task<Invoice?> GetInvoiceWithTeacherBankInfoAsync(Guid invoiceId, Guid studentId)
        {
            return await _context.Invoices
                .Include(i => i.Class)
                    .ThenInclude(c => c.Teacher) 
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.StudentId == studentId);
        }

        public async Task<bool> HasPendingTransactionAsync(Guid invoiceId)
        {
            return await _context.Transactions
                .AnyAsync(t => t.InvoiceId == invoiceId && t.Status == "Pending");
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }
    }
}
