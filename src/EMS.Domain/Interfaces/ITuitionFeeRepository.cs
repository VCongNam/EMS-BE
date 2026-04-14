using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface ITuitionFeeRepository
    {


        Task<bool> IsTeacherOwnsClassAsync(Guid classId, Guid teacherId);

        Task<Class?> GetClassByIdAsync(Guid classId);

        Task<IEnumerable<Class>> GetClassesWithStudentsByTeacherAsync(Guid teacherId);

        Task UpdateClassEnrollmentsAsync(IEnumerable<ClassEnrollment> enrollments);

        Task UpdateInvoicesAsync(IEnumerable<Invoice> invoices);

        Task UpdateInvoiceAsync(Invoice invoice);

        // --- Thống kê & Kiểm tra kỳ học ---
        Task<IEnumerable<ClassEnrollment>> GetActiveStudentsInClassAsync(Guid classId);

        Task<int> CountScheduledSessionsAsync(Guid classId, int month, int year);

        Task<int> CountStudentAttendanceAsync(Guid studentId, Guid classId, int month, int year);

        Task<int> CountExcusedAbsencesAsync(Guid studentId, Guid classId, int month, int year);

        Task<bool> HasAttendanceInMonthAsync(Guid classId, int month, int year);

        Task<bool> HasInvoicesForPeriodAsync(Guid classId, int month, int year);

        Task<Dictionary<Guid, int>> GetAttendanceCountsForClassPeriodAsync(Guid classId, DateTime startDate, DateTime endDate);

        // --- Phát hành & Truy vấn hóa đơn ---
        Task AddInvoicesAsync(IEnumerable<Invoice> invoices);

        Task<bool> AddInvoicesWithEnrollmentsAsync(IEnumerable<Invoice> invoices, IEnumerable<ClassEnrollment> enrollments, Guid classId, int periodMonth, int periodYear);

        Task<IEnumerable<Invoice>> GetClassInvoicesAsync(Guid classId, int month, int year);

        Task<Invoice?> GetInvoicesWithClassAsync(Guid invoiceId);

        Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId);

        Task<IEnumerable<Invoice>> GetInvoicesByClassAndPeriodAsync(Guid classId, int month, int year);

        Task<(List<Invoice> Items, int TotalCount)> GetInvoicesByClassAndPeriodPagedAsync(Guid classId, int month, int year, int page, int size, string? status = null, Guid? studentId = null);



        Task<IEnumerable<Transaction>> GetPendingTransactionsByTeacherAsync(Guid teacherId);

        Task<Transaction?> GetTransactionWithInvoiceAsync(Guid transactionId);

        Task<decimal> GetTotalPaidAmountAsync(Guid invoiceId);

        Task<bool> UpdateTransactionStatusAsync(Transaction transaction, Invoice? invoice);

        Task<IEnumerable<Transaction>> GetTransactionHistoryByTeacherAsync(Guid teacherId, DateTime? fromDate, DateTime? toDate);



        Task<(List<(Invoice Invoice, Transaction? LatestTransaction)> Items, int TotalCount)> GetStudentInvoicesAsync(Guid studentId, int page, int size, Guid? classId);

        Task<(Invoice? Invoice, Transaction? LatestTransaction, List<Attendance> Attendances)> GetInvoiceDetailAsync(Guid invoiceId, Guid studentId);

        Task<Invoice?> GetInvoiceWithTeacherBankInfoAsync(Guid invoiceId, Guid studentId);

        Task<bool> HasPendingTransactionAsync(Guid invoiceId);

        Task AddTransactionAsync(Transaction transaction);

        Task<List<Transaction>> GetTransactionsByStudentIdAsync(Guid studentId, Guid? classId);

        Task<Transaction?> GetTransactionDetailAsync(Guid transactionId, Guid studentId);


        // =========================================================
        // ⚙️ HỆ THỐNG & BACKGROUND SERVICE (System)
        // =========================================================

        Task<IEnumerable<Class>> GetAllClassesWithStudentsAsync();








        Task<IEnumerable<(Guid StudentId, string StudentName, string? AvatarUrl, Guid? InvoiceId, int SessionCount, decimal TotalAmount, decimal PaidAmount, DateTime? DueDate, string Status)>>
            GetPostpaidStudentInvoicesAsync(Guid classId, int month, int year);

        // Hàm này có thêm trường CreditBalance
        Task<IEnumerable<(Guid StudentId, string StudentName, string? AvatarUrl, Guid? InvoiceId, int SessionCount, decimal CreditBalance, decimal TotalAmount, decimal PaidAmount, DateTime? DueDate, string Status)>>
            GetPrepaidStudentInvoicesAsync(Guid classId, int month, int year);


        Task<(decimal Expected, decimal Actual)> GetClassPeriodRevenueAsync(Guid classId, int month, int year);

        Task<IEnumerable<Class>> GetTeacherClassesConfigAsync(Guid teacherId);
        Task UpdateClassFeeConfigAsync(Guid classId, string billingMethod, decimal fee, int deadlineDays);

        Task<Class?> GetClassConfigByIdAsync(Guid classId, Guid teacherId);


        Task ExtendInvoiceDueDateAsync(Guid invoiceId, int additionalDays, Guid teacherId);
        Task ExtendClassInvoicesDueDateAsync(Guid classId, int month, int year, int additionalDays);

    }
}
