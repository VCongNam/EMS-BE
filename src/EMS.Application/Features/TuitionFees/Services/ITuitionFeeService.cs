using EMS.Application.Features.TuitionFees.Dtos;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Services
{
    public interface ITuitionFeeService
    {
        Task<List<InvoicePreviewDto>> GetInvoicesPreviewAsync(Guid classId, int month, int year);
        Task ConfirmAndGenerateInvoicesAsync(Guid classId, ConfirmInvoicesDto dto);
        Task<InvoicePreviewDto> GetStudentFinalInvoicePreviewAsync(Guid classId, Guid studentId, int month, int year);
        Task ConfirmStudentFinalInvoiceAsync(Guid classId, Guid studentId, ConfirmSingleInvoiceDto dto);
        Task ExtendInvoiceDueDateAsync(Guid invoiceId, int additionalDays);
        Task ExtendClassInvoicesDueDateAsync(Guid classId, ExtendClassInvoicesDto request);

        Task<IEnumerable<PendingTransactionDto>> GetPendingTransactionsAsync();
        Task ReviewTransactionAsync(Guid transactionId, bool isApproved, string? note);
        Task<IEnumerable<TransactionHistoryDto>> GetTransactionHistoryAsync(DateTime? from, DateTime? to);
        Task UndoTransactionAsync(Guid transactionId);

        Task<IEnumerable<GlobalInvoiceRecordDto>> GetInvoicesListAsync(Guid? classId, int month, int year);
        Task<IEnumerable<ClassFeeConfigDto>> GetClassFeeConfigsAsync();
        Task UpdateClassFeeAsync(Guid classId, UpdateClassFeeConfigDto dto);
        Task<ClassFeeConfigDto> GetClassFeeConfigAsync(Guid classId);
        Task ExtendInvoiceAsync(Guid invoiceId, ExtendInvoiceDto dto);
        Task ExtendClassInvoicesAsync(Guid classId, ExtendClassInvoicesDto dto);

        Task<IEnumerable<ClassTuitionReportDto>> GetClassesOverviewAsync(int month, int year);
        Task<IEnumerable<Class>> GetClassesOverviewEntitiesAsync(Guid teacherId, int month, int year);
        Task<TuitionSummaryDto> GetTuitionSummaryAsync(Guid? classId, int month, int year);
        Task<List<ClassInvoiceReminderDto>> GetPendingInvoiceRemindersAsync(int month, int year);
        Task<IEnumerable<FullTransactionHistoryDto>> GetHistoryFullAsync(int month, int year);
        Task<TuitionDashboardDto> GetDashboardDataAsync(int month, int year);
        Task<IEnumerable<FullTransactionHistoryDto>> GetTransactionsByClassAsync(Guid classId, int month, int year);
        Task<IEnumerable<FullTransactionHistoryDto>> GetClassTransactionsByPeriodAsync(Guid classId, int month, int year);
        Task<IEnumerable<FullTransactionHistoryDto>> GetStudentTransactionsAsync(Guid studentId, Guid? classId = null);
        Task<PaymentQrDto> GetPaymentQrCodeForTeacherAsync(Guid invoiceId, Guid studentId);

        Task ReportCashPaymentAsync(Guid invoiceId, PayCashDto dto);
        Task SendOverdueRemindersAsync(Guid classId);

    }
}