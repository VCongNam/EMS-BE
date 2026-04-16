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


        // =========================================================
        // 🎯 MÀN 2: QUẢN LÝ LỚP & HÓA ĐƠN (Class Hub & Invoices)
        // =========================================================

        public async Task<bool> IsTeacherOwnsClassAsync(Guid classId, Guid teacherId)
        {
            return await context.Classes
                .AnyAsync(c => c.ClassId == classId && c.TeacherId == teacherId && c.IsDeleted != true);
        }

        public async Task<Class?> GetClassByIdAsync(Guid classId)
        {
            return await context.Classes.FirstOrDefaultAsync(c => c.ClassId == classId && c.IsDeleted != true);
        }

        public async Task<IEnumerable<Class>> GetClassesWithStudentsByTeacherAsync(Guid teacherId)
        {
            return await context.Classes
                .Include(c => c.ClassEnrollments.Where(e => e.Status == "Active"))
                .Where(c => c.TeacherId == teacherId && c.IsDeleted != true)
                .ToListAsync();
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

        public async Task UpdateInvoicesAsync(IEnumerable<Invoice> invoices)
        {
            context.Invoices.UpdateRange(invoices);
            await context.SaveChangesAsync();
        }

        public async Task UpdateInvoiceAsync(Invoice invoice)
        {
            context.Invoices.Update(invoice);
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


        public async Task<Dictionary<Guid, int>> GetAttendanceCountsForClassPeriodAsync(Guid classId, DateTime startDate, DateTime endDate)
        {
            var query = context.Attendances
                .Include(a => a.Session)
                .Where(a => a.Session.ClassId == classId && a.Session.Date >= DateOnly.FromDateTime(startDate) && a.Session.Date <= DateOnly.FromDateTime(endDate) && a.Status == "Present")
                .GroupBy(a => a.StudentId)
                .Select(g => new { StudentId = g.Key, Count = g.Count() });

            var list = await query.ToListAsync();
            return list.ToDictionary(x => x.StudentId, x => x.Count);
        }

        public async Task AddInvoicesAsync(IEnumerable<Invoice> invoices)
        {
            await context.Invoices.AddRangeAsync(invoices);
            await context.SaveChangesAsync();
        }

        public async Task<bool> AddInvoicesWithEnrollmentsAsync(IEnumerable<Invoice> invoices, IEnumerable<ClassEnrollment> enrollments, Guid classId, int periodMonth, int periodYear)
        {
            using var dbTrans = await context.Database.BeginTransactionAsync();
            try
            {
                var exists = await context.Invoices.AnyAsync(i => i.ClassId == classId && i.PeriodMonth == periodMonth && i.PeriodYear == periodYear);
                if (exists)
                {
                    await dbTrans.RollbackAsync();
                    return false;
                }

                if (invoices != null && invoices.Any())
                {
                    await context.Invoices.AddRangeAsync(invoices);
                    await context.SaveChangesAsync();
                }

                if (enrollments != null && enrollments.Any())
                {
                    context.ClassEnrollments.UpdateRange(enrollments);
                    await context.SaveChangesAsync();
                }

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
                .Include(i => i.Transactions.Where(t => t.Status == "Successful"))
                .Where(i => i.ClassId == classId && i.PeriodMonth == month && i.PeriodYear == year && i.IsDeleted != true).ToListAsync();
        }

        public async Task<Invoice?> GetInvoicesWithClassAsync(Guid invoiceId)
        {
            return await context.Invoices
                .Include(i => i.Class)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.IsDeleted != true);
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId)
        {
            return await context.Invoices
                .Include(i => i.Class)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByClassAndPeriodAsync(Guid classId, int month, int year)
        {
            return await context.Invoices
               .Where(i => i.ClassId == classId && i.PeriodMonth == month && i.PeriodYear == year && i.IsDeleted != true)
               .ToListAsync();
        }

        public async Task<(List<Invoice> Items, int TotalCount)> GetInvoicesByClassAndPeriodPagedAsync(Guid classId, int month, int year, int page, int size, string? status = null, Guid? studentId = null)
        {
            if (page < 1) page = 1;
            if (size < 1) size = 20;

            var query = context.Invoices
                .AsNoTracking()
                .Include(i => i.Student).ThenInclude(s => s.Account)
                .Include(i => i.Transactions)
                .Include(i => i.Class)
                .Where(i => i.ClassId == classId && i.PeriodMonth == month && i.PeriodYear == year && i.IsDeleted != true);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(i => i.Status != null && i.Status.ToLower() == status.ToLower());
            }

            if (studentId.HasValue)
            {
                query = query.Where(i => i.StudentId == studentId.Value);
            }

            var total = await query.CountAsync();
            var items = await query.OrderBy(i => i.DueDate).Skip((page - 1) * size).Take(size).ToListAsync();

            return (items, total);
        }


        // =========================================================
        // 🔍 MÀN 3: DUYỆT GIAO DỊCH & LỊCH SỬ (Queue & History)
        // =========================================================

        public async Task<IEnumerable<Transaction>> GetPendingTransactionsByTeacherAsync(Guid teacherId)
        {
            return await context.Transactions
                .Include(t => t.Invoice).ThenInclude(i => i.Student).ThenInclude(s => s.Account)
                .Include(t => t.Invoice).ThenInclude(i => i.Class)
                .Where(t => t.Status == "Pending" && t.Invoice.Class.TeacherId == teacherId)
                .ToListAsync();
        }

        public async Task<Transaction?> GetTransactionWithInvoiceAsync(Guid transactionId)
        {
            return await context.Transactions
                .Include(t => t.Invoice).ThenInclude(i => i.Student).ThenInclude(s => s.Account)
                .Include(t => t.Invoice).ThenInclude(i => i.Class)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<decimal> GetTotalPaidAmountAsync(Guid invoiceId)
        {
            return await context.Transactions
                .Where(t => t.InvoiceId == invoiceId && t.Status == "Successful").SumAsync(t => t.AmountPaid);
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

        public async Task<IEnumerable<Transaction>> GetTransactionHistoryByTeacherAsync(Guid teacherId, DateTime? from, DateTime? to)
        {
            var query = context.Transactions
                .Include(t => t.Invoice).ThenInclude(i => i.Student)
                .Include(t => t.Invoice).ThenInclude(i => i.Class)
                .Where(t => t.Invoice.Class.TeacherId == teacherId && (t.Status == "Successful" || t.Status == "Failed"));

            if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(t => t.CreatedAt <= to.Value);

            return await query.OrderByDescending(t => t.UpdatedAt).ToListAsync();
        }


        // =========================================================
        // 🎓 PHẦN DÀNH CHO HỌC SINH (Student Portal)
        // =========================================================

        public async Task<(List<(Invoice Invoice, Transaction? LatestTransaction)> Items, int TotalCount)> GetStudentInvoicesAsync(Guid studentId, int page, int size, Guid? classId)
        {
            var query = context.Invoices
                .Include(i => i.Class)
                .Where(i => i.StudentId == studentId)
                .AsNoTracking();

            if (classId.HasValue) query = query.Where(i => i.ClassId == classId.Value);

            int totalCount = await query.CountAsync();
            var dbResult = await query.OrderBy(i => i.DueDate).Skip((page - 1) * size).Take(size)
                .Select(i => new { Invoice = i, LatestTransaction = context.Transactions.Where(t => t.InvoiceId == i.InvoiceId).OrderByDescending(t => t.CreatedAt).FirstOrDefault() })
                .ToListAsync();

            var items = dbResult.Select(x => (x.Invoice, x.LatestTransaction)).ToList();
            return (items, totalCount);
        }

        public async Task<(Invoice? Invoice, Transaction? LatestTransaction, List<Attendance> Attendances)> GetInvoiceDetailAsync(Guid invoiceId, Guid studentId)
        {
            var invoice = await context.Invoices.Include(i => i.Class).AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.StudentId == studentId);

            if (invoice == null) return (null, null, new List<Attendance>());

            var latestTransaction = await context.Transactions.AsNoTracking()
                .Where(t => t.InvoiceId == invoiceId).OrderByDescending(t => t.CreatedAt).FirstOrDefaultAsync();

            var attendances = await context.Attendances.Include(a => a.Session).AsNoTracking()
                .Where(a => a.InvoiceId == invoiceId && a.StudentId == studentId).OrderBy(a => a.Session.Date).ToListAsync();

            return (invoice, latestTransaction, attendances);
        }

        public async Task<Invoice?> GetInvoiceWithTeacherBankInfoAsync(Guid invoiceId, Guid studentId)
        {
            return await context.Invoices.Include(i => i.Class).ThenInclude(c => c.Teacher).AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.StudentId == studentId);
        }

        public async Task<bool> HasPendingTransactionAsync(Guid invoiceId)
        {
            return await context.Transactions
                .AnyAsync(t => t.InvoiceId == invoiceId && t.Status == "Pending" || t.Status == "Rejected");
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            await context.Transactions.AddAsync(transaction);
            await context.SaveChangesAsync();
        }

        public async Task<List<Transaction>> GetTransactionsByStudentIdAsync(Guid studentId, Guid? classId)
        {
            var result = await context.Transactions.Include(t => t.Invoice)
                .Where(t => t.Invoice.StudentId == studentId).OrderByDescending(t => t.CreatedAt).ToListAsync();

            if (classId.HasValue) result = result.Where(i => i.Invoice.ClassId == classId.Value).ToList();
            return result;
        }

        public async Task<Transaction?> GetTransactionDetailAsync(Guid transactionId, Guid studentId)
        {
            return await context.Transactions.Include(t => t.Invoice)
                .Where(t => t.TransactionId == transactionId && t.Invoice.StudentId == studentId && t.Invoice.IsDeleted == false)
                .FirstOrDefaultAsync();
        }


        // =========================================================
        // ⚙️ HỆ THỐNG & BACKGROUND SERVICE (System)
        // =========================================================

        public async Task<IEnumerable<Class>> GetAllClassesWithStudentsAsync()
        {
            return await context.Classes
                .Include(c => c.ClassEnrollments.Where(e => e.Status == "Active"))
                .ToListAsync();
        }






        public async Task<IEnumerable<Invoice>> GetInvoicesByFilterAsync(Guid teacherId, Guid? classId, int month, int year)
        {
            var query = context.Invoices
                .Include(i => i.Class)
                .Include(i => i.Student)
                    .ThenInclude(s => s.Account)
                // Kéo theo các giao dịch để tính số tiền đã nộp
                .Include(i => i.Transactions.Where(t => t.Status == "Successful" || t.Status == "Completed"))
                .Where(i => i.Class.TeacherId == teacherId
                         && i.PeriodMonth == month
                         && i.PeriodYear == year
                         && i.IsDeleted != true);

            // Lọc theo ClassId nếu có truyền vào từ FE
            if (classId.HasValue && classId != Guid.Empty)
            {
                query = query.Where(i => i.ClassId == classId.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<(decimal Expected, decimal Actual)> GetClassPeriodRevenueAsync(Guid classId, int month, int year)
        {
            // 1. Tính tổng Doanh thu dự kiến (Expected) từ tất cả hóa đơn hợp lệ trong kỳ
            var expected = await context.Invoices
                .Where(i => i.ClassId == classId
                         && i.PeriodMonth == month
                         && i.PeriodYear == year
                         && i.IsDeleted != true)
                .SumAsync(i => i.Amount);

            // 2. Tính tổng Doanh thu đã thu (Actual) từ các giao dịch thành công của các hóa đơn đó
            var actual = await context.Transactions
                .Where(t => t.Invoice.ClassId == classId
                         && t.Invoice.PeriodMonth == month
                         && t.Invoice.PeriodYear == year
                         && t.Invoice.IsDeleted != true
                         && (t.Status == "Successful" || t.Status == "Completed"))
                .SumAsync(t => t.AmountPaid);

            return (expected, actual);
        }


        public async Task<IEnumerable<Class>> GetTeacherClassesConfigAsync(Guid teacherId)
        {
            // Chỉ lấy các lớp đang hoạt động của giáo viên đó
            return await context.Classes
                .Where(c => c.TeacherId == teacherId && c.IsDeleted != true && c.Status != "Completed")
                .ToListAsync();
        }

        public async Task UpdateClassFeeConfigAsync(Guid classId, string billingMethod, decimal fee, int deadlineDays)
        {
            var classEntity = await context.Classes.FirstOrDefaultAsync(c => c.ClassId == classId);
            if (classEntity != null)
            {
                classEntity.BillingMethod = billingMethod;
                classEntity.TuitionFee = fee;
                classEntity.PaymentDeadlineDays = deadlineDays;
                classEntity.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();
            }
        }

        public async Task<Class?> GetClassConfigByIdAsync(Guid classId, Guid teacherId)
        {
            // Tìm lớp theo ID, đồng thời check luôn quyền sở hữu của giáo viên đó
            return await context.Classes
                .FirstOrDefaultAsync(c => c.ClassId == classId
                                       && c.TeacherId == teacherId
                                       && c.IsDeleted != true);
        }

        public async Task ExtendInvoiceDueDateAsync(Guid invoiceId, int additionalDays, Guid teacherId)
        {
            // Tìm hóa đơn kèm theo thông tin Lớp để check quyền của giáo viên
            var invoice = await context.Invoices
                .Include(i => i.Class)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.IsDeleted != true);

            if (invoice == null)
                throw new KeyNotFoundException("Không tìm thấy hóa đơn này.");

            if (invoice.Class.TeacherId != teacherId)
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên hóa đơn của giáo viên khác.");

            if (invoice.Status == "Paid" || invoice.Status == "Cancelled")
                throw new InvalidOperationException($"Không thể gia hạn hóa đơn đang ở trạng thái {invoice.Status}.");

            // Xóa bỏ đoạn (invoice.DueDate ?? DateTime.UtcNow) đi
            invoice.DueDate = invoice.DueDate.AddDays(additionalDays);

            // Nếu đang quá hạn thì chuyển lại thành Pending
            if (invoice.Status == "Overdue")
            {
                invoice.Status = "Pending";
            }

            invoice.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        public async Task ExtendClassInvoicesDueDateAsync(Guid classId, int month, int year, int additionalDays)
        {
            var pendingInvoices = await context.Invoices
                .Where(i => i.ClassId == classId
                         // Đã xóa i.DueDate != null và đuôi .Value
                         && i.DueDate.Month == month
                         && i.DueDate.Year == year
                         && i.IsDeleted != true
                         && (i.Status == "Pending" || i.Status == "Overdue"))
                .ToListAsync();

            if (!pendingInvoices.Any())
                throw new InvalidOperationException("Không có hóa đơn nào đang nợ trong kỳ này để gia hạn.");

            foreach (var inv in pendingInvoices)
            {
                // Đã xóa .Value ở đây
                inv.DueDate = inv.DueDate.AddDays(additionalDays);
                if (inv.Status == "Overdue") inv.Status = "Pending";
                inv.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }


        
       public async Task<IEnumerable<Class>> GetClassesWithDataAsync(Guid teacherId, int month, int year)
        {
            return await context.Classes
                .Include(c => c.ClassEnrollments.Where(ce => ce.Status == "Active"))
                .Include(c => c.Invoices.Where(i => i.PeriodMonth == month && i.PeriodYear == year && i.IsDeleted != true))
                .Where(c => c.TeacherId == teacherId && c.IsDeleted != true && c.Status != "Archived")
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByPeriodAsync(Guid teacherId, Guid? classId, int month, int year)
        {
            var query = context.Invoices
                .Where(i => i.Class.TeacherId == teacherId && i.PeriodMonth == month && i.PeriodYear == year && i.IsDeleted != true);

            if (classId.HasValue && classId != Guid.Empty)
                query = query.Where(i => i.ClassId == classId.Value);

            return await query.ToListAsync();
        }



        public Task<IEnumerable<(Guid ClassId, string ClassName, string BillingMethod, Guid StudentId, string StudentName, string? AvatarUrl, Guid? InvoiceId, int SessionCount, decimal CreditBalance, decimal TotalAmount, decimal PaidAmount, DateTime? DueDate, string Status)>> GetAllStudentsInvoicesByMonthAsync(Guid teacherId, int month, int year)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Class>> GetActiveClassesAsync(Guid teacherId)
        {
            return await context.Classes
                .Where(c => c.TeacherId == teacherId && c.IsDeleted != true && c.Status != "Archived")
                .ToListAsync();
        }

        public async Task<bool> HasInvoicesForPeriodAsync(Guid classId, int month, int year)
        {
            return await context.Invoices
                .AnyAsync(i => i.ClassId == classId
                            && i.PeriodMonth == month
                            && i.PeriodYear == year
                            && i.IsDeleted != true);
        }
        public async Task<IEnumerable<Transaction>> GetFullTransactionHistoryAsync(Guid teacherId)
        {
            return await context.Transactions
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Student)
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Class)
                .Where(t => t.Invoice.Class.TeacherId == teacherId && t.Invoice.IsDeleted != true)
                .OrderByDescending(t => t.CreatedAt) // Mới nhất lên đầu
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetInvoicesByPeriodAsync(Guid teacherId, int month, int year)
        {
            return await context.Invoices
                .Include(i => i.Class)
                .Where(i => i.Class.TeacherId == teacherId
                         && i.PeriodMonth == month
                         && i.PeriodYear == year
                         // Lọc hóa đơn hợp lệ (Không tính hóa đơn đã hủy)
                         && i.Status != "Cancelled"
                         && i.Class.Status != "Archived"
                         && i.IsDeleted != true)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetSuccessfulTransactionsByPeriodAsync(Guid teacherId, int month, int year)
        {
            return await context.Transactions
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Class)
                .Where(t => t.Invoice.Class.TeacherId == teacherId
                         && t.Invoice.PeriodMonth == month
                         && t.Invoice.PeriodYear == year
                         // CHỈ lấy các giao dịch nộp tiền THÀNH CÔNG
                         && t.Status == "Successful"
                         && t.Invoice.IsDeleted != true)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByClassAsync(Guid classId, Guid teacherId)
        {
            return await context.Transactions
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Student)
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Class)
                .Where(t => t.Invoice.ClassId == classId
                         && t.Invoice.Class.TeacherId == teacherId
                         && t.Invoice.IsDeleted != true)
                .OrderByDescending(t => t.CreatedAt) // Giao dịch mới nhất lên đầu
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByClassAndPeriodAsync(Guid classId, Guid teacherId, int month, int year)
        {
            return await context.Transactions
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Student)
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Class)
                .Where(t => t.Invoice.ClassId == classId
                         && t.Invoice.Class.TeacherId == teacherId
                         && t.Invoice.PeriodMonth == month
                         && t.Invoice.PeriodYear == year
                         && t.Invoice.IsDeleted != true)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
