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
    public class TuitionFeeService : ITuitionFeeService
    {
        private readonly ITuitionFeeRepository tuitionFeeRepository;

        public TuitionFeeService(ITuitionFeeRepository tuitionFeeRepository)
        {
            this.tuitionFeeRepository = tuitionFeeRepository;
        }

        public async Task UpdateTuitionFeeAsync(Guid classId, UpdateTuitionFeeDto request)
        {
            var classEntity = await tuitionFeeRepository.GetClassByIdAsync(classId);
            if (classEntity == null) throw new Exception("Class not found.");

            classEntity.TuitionFee = request.TuitionFee;
            classEntity.UpdatedAt = DateTime.UtcNow;

            await tuitionFeeRepository.UpdateClassAsync(classEntity);
        }

        public async Task UpdateTuitionDeadlineAsync(Guid classId, UpdateTuitionFeeDeadlineDto request)
        {
            var invoices = await tuitionFeeRepository.GetInvoicesByClassAndPeriodAsync(classId, request.PeriodMonth, request.PeriodYear);

            if (!invoices.Any())
                throw new Exception($"No invoices found for Class {classId} in {request.PeriodMonth}/{request.PeriodYear}.");

            foreach (var invoice in invoices)
            {
                if (invoice.Status == "Pending" || invoice.Status == "Partial")
                {
                    invoice.DueDate = request.DueDate;
                    invoice.UpdatedAt = DateTime.UtcNow;
                }
            }

            await tuitionFeeRepository.UpdateInvoicesAsync(invoices);
        }

        public async Task<bool> ReviewTransactionAsync(Guid transactionId, ReviewTransactionDto request)
        {
            var transaction = await tuitionFeeRepository.GetTransactionWithInvoiceAsync(transactionId);
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
            return await tuitionFeeRepository.UpdateTransactionStatusAsync(transaction, invoiceToUpdate);
        }
    }
}
