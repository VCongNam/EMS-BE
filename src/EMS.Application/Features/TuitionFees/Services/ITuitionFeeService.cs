using EMS.Application.Features.TuitionFees.Dtos;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Services
{
    public interface ITuitionFeeService
    {
        Task GenerateInvoicesForClassAsync(Guid classId, GenerateInvoiceDto request, Guid teacherId);

        Task ReconcilePrepaidClassAsync(Guid classId, int month, int year, Guid teacherId);

        Task ExtendInvoiceDueDateAsync(Guid invoiceId, int additionalDays, Guid teacherId);

        Task ExtendClassInvoicesDueDateAsync(Guid classId, ExtendClassInvoicesDto request, Guid teacherId);

        Task<IEnumerable<PendingTransactionDto>> GetPendingTransactionsAsync(Guid teacherId);

        Task ReviewTransactionAsync(Guid transactionId, bool isApproved, Guid approverId, string? note);

        Task<IEnumerable<TransactionHistoryDto>> GetTransactionHistoryAsync(Guid teacherId, DateTime? from, DateTime? to);

        Task UndoTransactionAsync(Guid transactionId, Guid teacherId);

        Task<IEnumerable<GlobalInvoiceRecordDto>> GetInvoicesListAsync(Guid? classId, int month, int year);

        Task<IEnumerable<ClassFeeConfigDto>> GetClassFeeConfigsAsync();
        Task UpdateClassFeeAsync(Guid classId, UpdateClassFeeConfigDto dto);

        Task<ClassFeeConfigDto> GetClassFeeConfigAsync(Guid classId);
        Task ExtendInvoiceAsync(Guid invoiceId, ExtendInvoiceDto dto);
        Task ExtendClassInvoicesAsync(Guid classId, ExtendClassInvoicesDto dto);
        Task<IEnumerable<Class>> GetClassesOverviewEntitiesAsync(Guid teacherId, int month, int year);
        Task<IEnumerable<ClassTuitionReportDto>> GetClassesOverviewAsync(int month, int year);

        Task<TuitionSummaryDto> GetTuitionSummaryAsync(Guid? classId, int month, int year);

        Task<List<ClassInvoiceReminderDto>> GetPendingInvoiceRemindersAsync(int month, int year);

        Task<IEnumerable<FullTransactionHistoryDto>> GetHistoryFullAsync();

        Task<TuitionDashboardDto> GetDashboardDataAsync(int month, int year);

        Task<IEnumerable<FullTransactionHistoryDto>> GetTransactionsByClassAsync(Guid classId);
        Task<IEnumerable<FullTransactionHistoryDto>> GetClassTransactionsByPeriodAsync(Guid classId, int month, int year);
    }
}
