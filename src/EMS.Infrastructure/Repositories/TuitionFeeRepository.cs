using DocumentFormat.OpenXml.InkML;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Repositories
{
    public class TuitionFeeRepository : ITuitionFeeRepository
    {
        private readonly ApplicationDbContext context;

        public TuitionFeeRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        // --- 1. KIỂM TRA QUYỀN SỞ HỮU LỚP ---
        public async Task<bool> IsTeacherOwnsClassAsync(Guid classId, Guid teacherId)
        {
            return await context.Classes
                .AnyAsync(c => c.ClassId == classId && c.TeacherId == teacherId && c.IsDeleted != true);
        }

        // --- 2. LẤY DỮ LIỆU RIÊNG CỦA TỪNG GIÁO VIÊN ---
        public async Task<IEnumerable<Class>> GetClassesWithStudentsByTeacherAsync(Guid teacherId)
        {
            return await context.Classes
                .Include(c => c.ClassEnrollments.Where(e => e.Status == "Active"))
                .Where(c => c.TeacherId == teacherId && c.IsDeleted != true)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetPendingTransactionsByTeacherAsync(Guid teacherId)
        {
            return await context.Transactions
                .Include(t => t.Invoice).ThenInclude(i => i.Student).ThenInclude(s => s.Account)
                .Include(t => t.Invoice).ThenInclude(i => i.Class)
                .Where(t => t.Status == "Pending" && t.Invoice.Class.TeacherId == teacherId)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueByTeacherAsync(Guid teacherId)
        {
            return await context.Transactions
                .Where(t => t.Status == "Completed" && t.Invoice.Class.TeacherId == teacherId)
                .SumAsync(t => t.AmountPaid);
        }

        public async Task<int> CountInvoicesByStatusForTeacherAsync(string status, Guid teacherId)
        {
            return await context.Invoices
                .CountAsync(i => i.Status == status && i.Class.TeacherId == teacherId);
        }

        // --- 3. CÁC HÀM THAO TÁC CHUNG (Giữ nguyên) ---
        public async Task<Class?> GetClassByIdAsync(Guid classId)
        {
            return await context.Classes.FirstOrDefaultAsync(c => c.ClassId == classId && c.IsDeleted != true);
        }

        public async Task UpdateClassAsync(Class c)
        {
            context.Classes.Update(c);
            await context.SaveChangesAsync();
        }

        public async Task UpdateClassEnrollmentsAsync(IEnumerable<ClassEnrollment> e)
        {
            context.ClassEnrollments.UpdateRange(e);
            await context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ClassEnrollment>> GetActiveStudentsInClassAsync(Guid classId)
        {
            return await context.ClassEnrollments
                .Where(e => e.ClassId == classId && e.Status == "Active").ToListAsync();
        }

        public async Task<int> CountScheduledSessionsAsync(Guid classId, int month, int year)
        {
            return await context.Sessions
                .CountAsync(s => s.ClassId == classId && s.Date.Month == month && s.Date.Year == year && s.IsDeleted != true);
        }

        public async Task<int> CountStudentAttendanceAsync(Guid studentId, Guid classId, int month, int year)
        {
            return await context.Attendances.Include(a => a.Session)
                .CountAsync(a => a.StudentId == studentId && a.Session.ClassId == classId && a.Session.Date.Month == month && a.Session.Date.Year == year && a.Status == "Present");
        }

        public async Task<int> CountExcusedAbsencesAsync(Guid studentId, Guid classId, int month, int year)
        {
            return await context.Attendances.Include(a => a.Session)
                .CountAsync(a => a.StudentId == studentId && a.Session.ClassId == classId && a.Session.Date.Month == month && a.Session.Date.Year == year && a.Status == "Absent" && a.IsExcused == true);
        }

        public async Task<bool> HasAttendanceInMonthAsync(Guid classId, int month, int year)
        {
            return await context.Attendances.Include(a => a.Session)
                .AnyAsync(a => a.Session.ClassId == classId && a.Session.Date.Month == month && a.Session.Date.Year == year);
        }

        public async Task<bool> HasInvoicesForPeriodAsync(Guid classId, int month, int year)
        {
            return await context.Invoices.AnyAsync(i => i.ClassId == classId && i.PeriodMonth == month && i.PeriodYear == year);
        }

        public async Task AddInvoicesAsync(IEnumerable<Invoice> invoices)
        {
            await context.Invoices.AddRangeAsync(invoices);
            await context.SaveChangesAsync();
        }

        public async Task<Transaction?> GetTransactionWithInvoiceAsync(Guid transactionId)
        {
            return await context.Transactions.Include(t => t.Invoice)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<decimal> GetTotalPaidAmountAsync(Guid invoiceId)
        {
            return await context.Transactions
                .Where(t => t.InvoiceId == invoiceId && t.Status == "Completed").SumAsync(t => t.AmountPaid);
        }

        public async Task<bool> UpdateTransactionStatusAsync(Transaction t, Invoice? i)
        {
            using var dbTrans = await context.Database.BeginTransactionAsync();
            try
            {
                context.Transactions.Update(t);
                if (i != null) context.Invoices.Update(i);

                await context.SaveChangesAsync();
                await dbTrans.CommitAsync();
                return true;
            }
            catch
            {
                await dbTrans.RollbackAsync();
                return false;
            }
        }

        public async Task<IEnumerable<Invoice>> GetClassInvoicesAsync(Guid classId, int month, int year)
        {
            return await context.Invoices
                .Include(i => i.Student).ThenInclude(s => s.Account)
                .Include(i => i.Transactions.Where(t => t.Status == "Completed"))
                .Where(i => i.ClassId == classId && i.PeriodMonth == month && i.PeriodYear == year).ToListAsync();
        }

        // --- 4. HÀM CHO BACKGROUND WORKER ---
        public async Task<IEnumerable<Class>> GetAllClassesWithStudentsAsync()
        {
            return await context.Classes
                .Include(c => c.ClassEnrollments.Where(e => e.Status == "Active"))
                .ToListAsync();
        }

        public async Task UpdateInvoicesAsync(IEnumerable<Invoice> invoices)
        {
            context.Invoices.UpdateRange(invoices);
            await context.SaveChangesAsync();
        }

        public async Task<(List<(Invoice Invoice, Transaction? LatestTransaction)> Items, int TotalCount)> GetStudentInvoicesAsync(Guid studentId, int page, int size, Guid? classId)
        {
            var query = context.Invoices
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
                    LatestTransaction = context.Transactions
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
            var invoice = await context.Invoices
                .Include(i => i.Class)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.StudentId == studentId);
            if (invoice == null) return (null, null, new List<Attendance>());
            var latestTransaction = await context.Transactions
                .AsNoTracking()
                .Where(t => t.InvoiceId == invoiceId)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            var attendances = await context.Attendances
                .Include(a => a.Session)
                .AsNoTracking()
                .Where(a => a.InvoiceId == invoiceId && a.StudentId == studentId)
                .OrderBy(a => a.Session.Date)
                .ToListAsync();
            return (invoice, latestTransaction, attendances);
        }

        public async Task<Invoice?> GetInvoiceWithTeacherBankInfoAsync(Guid invoiceId, Guid studentId)
        {
            return await context.Invoices
                .Include(i => i.Class)
                    .ThenInclude(c => c.Teacher)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.StudentId == studentId);
        }

        public async Task<bool> HasPendingTransactionAsync(Guid invoiceId)
        {
            return await context.Transactions
                .AnyAsync(t => t.InvoiceId == invoiceId && t.Status == "Pending");
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            await context.Transactions.AddAsync(transaction);
            await context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByClassAndPeriodAsync(Guid classId, int month, int year)
        {
            return await context.Invoices
               .Where(i => i.ClassId == classId && i.PeriodMonth == month && i.PeriodYear == year && i.IsDeleted != true)
               .ToListAsync();
        }


        // =======================================================
        // PHẦN BỔ SUNG: DASHBOARD & GIA HẠN (SỬ DỤNG TUPLE, KHÔNG DTO)
        // =======================================================
        public async Task<int> GetTotalActiveStudentsByTeacherAsync(Guid teacherId)
        {
            return await context.ClassEnrollments
                .Where(e => e.Status == "Active" && e.Class.TeacherId == teacherId && !e.Class.IsDeleted.Value)
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();
        }

        public async Task<IEnumerable<(Guid ClassId, string ClassName, int StudentCount, decimal ExpectedRevenue, decimal ActualRevenue)>> GetClassFinancialSummariesAsync(Guid teacherId)
        {
            var classes = await context.Classes
                .Where(c => c.TeacherId == teacherId && c.IsDeleted != true)
                .Select(c => new
                {
                    c.ClassId,
                    c.ClassName,
                    StudentCount = c.ClassEnrollments.Count(e => e.Status == "Active"),
                    Expected = c.Invoices.Sum(i => i.Amount),
                    Actual = c.Invoices.SelectMany(i => i.Transactions).Where(t => t.Status == "Completed").Sum(t => t.AmountPaid)
                })
                .ToListAsync();

            return classes.Select(c => (c.ClassId, c.ClassName, c.StudentCount, c.Expected, c.Actual));
        }

        public async Task<IEnumerable<(string MonthLabel, decimal Revenue)>> GetRevenueTrendAsync(Guid teacherId, int monthsToLookBack)
        {
            var startDate = DateTime.UtcNow.AddMonths(-monthsToLookBack);
            var transactions = await context.Transactions
                .Where(t => t.Status == "Completed" && t.Invoice.Class.TeacherId == teacherId && t.PaidDate >= startDate)
                .ToListAsync();

            return transactions
                .GroupBy(t => new { t.PaidDate!.Value.Year, t.PaidDate.Value.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => ($"Tháng {g.Key.Month:D2}/{g.Key.Year.ToString().Substring(2)}", g.Sum(t => t.AmountPaid)));
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId)
        {
            return await context.Invoices
                .Include(i => i.Class)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        }

        public async Task UpdateInvoiceAsync(Invoice invoice)
        {
            context.Invoices.Update(invoice);
            await context.SaveChangesAsync();
        }

        // Transaction
        public async Task<List<Transaction>> GetTransactionsByStudentIdAsync(Guid studentId, Guid? classId)
        {
            var result = await context.Transactions
            .Include(t => t.Invoice)
            .Where(t => t.Invoice.StudentId == studentId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
            if (classId.HasValue)
            {
                result = result.Where(i => i.Invoice.ClassId == classId.Value).ToList();
            }
            return result;
        }

        public async Task<Transaction?> GetTransactionDetailAsync(Guid transactionId, Guid studentId)
        {
            return await context.Transactions
                .Include(t => t.Invoice)  
                .Where(t => t.TransactionId == transactionId
                         && t.Invoice.StudentId == studentId 
                         && t.Invoice.IsDeleted == false)
                .FirstOrDefaultAsync();
        }
    }
}
