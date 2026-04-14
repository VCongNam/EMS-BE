using EMS.Application.Features.TuitionFees.Dtos;
using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Services
{
    public interface ITuitionFeeService
    {

 


        Task<(IEnumerable<ClassInvoiceItemDto> Items, int TotalCount)> GetClassInvoicesForPeriodAsync(
            Guid classId, int month, int year, Guid teacherId, int page, int size, string? status = null, Guid? studentId = null);

        




        Task GenerateInvoicesForClassAsync(Guid classId, GenerateInvoiceDto request, Guid teacherId);

        Task ReconcilePrepaidClassAsync(Guid classId, int month, int year, Guid teacherId);

        Task ExtendInvoiceDueDateAsync(Guid invoiceId, int additionalDays, Guid teacherId);

        Task ExtendClassInvoicesDueDateAsync(Guid classId, ExtendClassInvoicesDto request, Guid teacherId);




        Task<IEnumerable<PendingTransactionDto>> GetPendingTransactionsAsync(Guid teacherId);

        Task ReviewTransactionAsync(Guid transactionId, bool isApproved, Guid approverId, string? note);

        Task<IEnumerable<TransactionHistoryDto>> GetTransactionHistoryAsync(Guid teacherId, DateTime? from, DateTime? to);

        Task UndoTransactionAsync(Guid transactionId, Guid teacherId);








        Task<IEnumerable<PostpaidStudentInvoiceDto>> GetPostpaidInvoicesAsync(Guid classId, int month, int year);
        Task<IEnumerable<PrepaidStudentInvoiceDto>> GetPrepaidInvoicesAsync(Guid classId, int month, int year);
        Task<ClassPeriodRevenueDto> GetClassRevenueReportAsync(Guid classId, int month, int year);

        Task<IEnumerable<ClassFeeConfigDto>> GetClassFeeConfigsAsync();
        Task UpdateClassFeeAsync(Guid classId, UpdateClassFeeConfigDto dto);

        Task<ClassFeeConfigDto> GetClassFeeConfigAsync(Guid classId);
        Task ExtendInvoiceAsync(Guid invoiceId, ExtendInvoiceDto dto);
        Task ExtendClassInvoicesAsync(Guid classId, ExtendClassInvoicesDto dto);

    }
}
