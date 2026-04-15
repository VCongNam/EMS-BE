using EMS.Application.Features.Assignments.DTOs;
using EMS.Application.Features.TuitionFees.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Services
{
    public interface IStudentTuitionService
    {
        Task<PagedResult<StudentTuitionDto>> GetMyTuitionAsync(TuitionFilter filter);
        Task<StudentTuitionInvoiceDetailDto> GetTuitionInvoiceDetailAsync(Guid invoiceId);
        Task<PaymentQrDto> GetPaymentQrCodeAsync(Guid invoiceId);
        Task<bool> UploadPaymentProofAsync(Guid invoiceId, ProofUploadDto request);
        Task<List<StudentTransactionViewDto>> GetMyTransactionsAsync(Guid? classId);
        Task<StudentTransactionDetailDto?> GetTransactionByIdAsync(Guid transactionId);
    }
}
