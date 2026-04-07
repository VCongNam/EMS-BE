using EMS.Application.Features.TuitionFees.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Services
{
    public interface ITuitionFeeService
    {
        Task<IEnumerable<TuitionFeeConfigDto>> GetTuitionFeeConfigsAsync(Guid teacherId);

        Task UpdateTuitionFeeAsync(Guid classId, UpdateTuitionFeeDto request, Guid teacherId);

        Task GenerateInvoicesForClassAsync(Guid classId, GenerateInvoiceDto request, Guid teacherId);

        Task ReconcilePrepaidClassAsync(Guid classId, int month, int year, Guid teacherId);

        Task<IEnumerable<PendingTransactionDto>> GetPendingTransactionsAsync(Guid teacherId);

        Task ReviewTransactionAsync(Guid transactionId, bool isApproved, Guid approverId, string? note);

        Task<ClassFinancialDetailDto> GetClassFinancialDetailAsync(Guid classId, int month, int year, Guid teacherId);

        Task<OverallReportDto> GetOverallReportAsync(Guid teacherId);
    }
}
