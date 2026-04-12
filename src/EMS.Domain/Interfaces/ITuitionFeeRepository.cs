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
        // --- CÁC HÀM XÁC THỰC VÀ LẤY DỮ LIỆU RIÊNG CỦA TỪNG GIÁO VIÊN ---
        Task<bool> IsTeacherOwnsClassAsync(Guid classId, Guid teacherId);
        Task<IEnumerable<Class>> GetClassesWithStudentsByTeacherAsync(Guid teacherId);
        Task<IEnumerable<Transaction>> GetPendingTransactionsByTeacherAsync(Guid teacherId);
        Task<decimal> GetTotalRevenueByTeacherAsync(Guid teacherId);
        Task<int> CountInvoicesByStatusForTeacherAsync(string status, Guid teacherId);

        // --- CÁC HÀM THAO TÁC DỮ LIỆU ---
        Task<Class?> GetClassByIdAsync(Guid classId);
        Task UpdateClassAsync(Class classEntity);
        Task UpdateClassEnrollmentsAsync(IEnumerable<ClassEnrollment> enrollments);

        Task<IEnumerable<ClassEnrollment>> GetActiveStudentsInClassAsync(Guid classId);
        Task<int> CountScheduledSessionsAsync(Guid classId, int month, int year);
        Task<int> CountStudentAttendanceAsync(Guid studentId, Guid classId, int month, int year);
        Task<int> CountExcusedAbsencesAsync(Guid studentId, Guid classId, int month, int year);

        Task<bool> HasAttendanceInMonthAsync(Guid classId, int month, int year);
        Task<bool> HasInvoicesForPeriodAsync(Guid classId, int month, int year);

        Task AddInvoicesAsync(IEnumerable<Invoice> invoices);
        // Atomically add invoices and update enrollments in a single transaction.
        Task<bool> AddInvoicesWithEnrollmentsAsync(IEnumerable<Invoice> invoices, IEnumerable<ClassEnrollment> enrollments, Guid classId, int periodMonth, int periodYear);

        // Get attendance counts for all students in a class within a period (start/end date)
        Task<Dictionary<Guid,int>> GetAttendanceCountsForClassPeriodAsync(Guid classId, DateTime startDate, DateTime endDate);

        Task<Transaction?> GetTransactionWithInvoiceAsync(Guid transactionId);
        Task<decimal> GetTotalPaidAmountAsync(Guid invoiceId);
        Task<bool> UpdateTransactionStatusAsync(Transaction transaction, Invoice? invoice);
        Task<IEnumerable<Invoice>> GetClassInvoicesAsync(Guid classId, int month, int year);
        Task<Invoice?> GetInvoicesWithClassAsync(Guid invoiceId);

        // --- HÀM DÀNH RIÊNG CHO BACKGROUND SERVICE (Hệ thống tự động) ---
        Task<IEnumerable<Class>> GetAllClassesWithStudentsAsync();



        Task<IEnumerable<Invoice>> GetInvoicesByClassAndPeriodAsync(Guid classId, int month, int year);
        // Paged & filtered version to support server-side paging/filters
        Task<(List<Invoice> Items, int TotalCount)> GetInvoicesByClassAndPeriodPagedAsync(Guid classId, int month, int year, int page, int size, string? status = null, Guid? studentId = null);
        Task UpdateInvoicesAsync(IEnumerable<Invoice> invoices);
        Task<(List<(Invoice Invoice, Transaction? LatestTransaction)> Items, int TotalCount)> GetStudentInvoicesAsync(
            Guid studentId, int page, int size, Guid? classId);
        Task<(Invoice? Invoice, Transaction? LatestTransaction, List<Attendance> Attendances)> GetInvoiceDetailAsync(Guid invoiceId, Guid studentId);
        Task<Invoice?> GetInvoiceWithTeacherBankInfoAsync(Guid invoiceId, Guid studentId);
        Task<bool> HasPendingTransactionAsync(Guid invoiceId);
        Task AddTransactionAsync(Transaction transaction);




        // --- BỔ SUNG: DASHBOARD & GIA HẠN ---
        Task<int> GetTotalActiveStudentsByTeacherAsync(Guid teacherId);
        Task<IEnumerable<(Guid ClassId, string ClassName, int StudentCount, decimal ExpectedRevenue, decimal ActualRevenue)>> GetClassFinancialSummariesAsync(Guid teacherId);
        Task<IEnumerable<(string MonthLabel, decimal Revenue)>> GetRevenueTrendAsync(Guid teacherId, int monthsToLookBack);
        Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId);
        Task UpdateInvoiceAsync(Invoice invoice);

        //Transaction
        Task<List<Transaction>> GetTransactionsByStudentIdAsync(Guid studentId, Guid? classId);
        Task<Transaction?> GetTransactionDetailAsync(Guid transactionId, Guid studentId);
    }
}
