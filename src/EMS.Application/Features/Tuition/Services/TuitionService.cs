using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Tuition.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tuition.Services
{
    public class TuitionService : ITuitionService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITuitionRepository _tuitionRepository;

        public TuitionService(ICurrentUserService currentUserService, ITuitionRepository tuitionRepository)
        {
            _currentUserService = currentUserService;
            _tuitionRepository = tuitionRepository;
        }

        public async Task<bool> ReviewTransactionAsync(Guid transactionId, ReviewTransactionDto request)
        {
            var transaction = await _tuitionRepository.GetTransactionWithInvoiceAsync(transactionId);
            if (transaction == null) throw new KeyNotFoundException("Giao dịch không tồn tại.");
            if (transaction.Status != "Pending") throw new Exception("Giao dịch này đã được xử lý trước đó.");
            Invoice? invoiceToUpdate = null;

            if (request.IsApproved)
            {
                transaction.Status = "Approved";

                if (transaction.Invoice != null)
                {
                    invoiceToUpdate = transaction.Invoice;
                    invoiceToUpdate.Status = "Paid";
                    invoiceToUpdate.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            { 
                transaction.Status = "Rejected";
            }

            transaction.UpdatedAt = DateTime.UtcNow;
            return await _tuitionRepository.UpdateTransactionStatusAsync(transaction, invoiceToUpdate);
        }
    }
}
