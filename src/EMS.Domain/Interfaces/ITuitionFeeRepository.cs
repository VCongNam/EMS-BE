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

        Task<Transaction?> GetTransactionWithInvoiceAsync(Guid transactionId);
        Task<decimal> GetTotalPaidAmountAsync(Guid invoiceId);
        Task<bool> UpdateTransactionStatusAsync(Transaction transaction, Invoice? invoice);
        Task<IEnumerable<Invoice>> GetClassInvoicesAsync(Guid classId, int month, int year);

        // --- HÀM DÀNH RIÊNG CHO BACKGROUND SERVICE (Hệ thống tự động) ---
        Task<IEnumerable<Class>> GetAllClassesWithStudentsAsync();
    }
}
