using EMS.Application.Features.Financials.Dtos;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Financials.Services
{
    public class FinancialService : IFinancialService
    {
        private readonly IFinancialRepository financialRepository;

        public FinancialService(IFinancialRepository financialRepository)
        {
            this.financialRepository = financialRepository;
        }

        public async Task<PaymentReportDto> GetOverallPaymentReportAsync()
        {
            var invoices = await financialRepository.GetAllInvoicesAsync();
            var recentTransactions = await financialRepository.GetRecentTransactionsAsync(10);

            var report = new PaymentReportDto();

            foreach (var invoice in invoices)
            {
                var paidAmount = invoice.Transactions
                    .Where(t => t.Status == "Completed" || t.Status == "Success")
                    .Sum(t => t.AmountPaid);

                report.TotalRevenue += paidAmount;

                var remaining = invoice.Amount - paidAmount;
                if (remaining > 0)
                {
                    report.TotalPendingAmount += remaining;
                }

                if (invoice.Status == "Paid")
                    report.TotalPaidInvoices++;
                else
                    report.TotalPendingInvoices++;
            }

            report.RecentTransactions = recentTransactions.Select(t => new TransactionDto
            {
                TransactionId = t.TransactionId,
                StudentName = t.Invoice?.Student?.StudentNavigation?.FullName ?? null!,
                ClassName = t.Invoice?.Class?.ClassName ?? null!,
                AmountPaid = t.AmountPaid,
                PaymentMethod = t.PaymentMethod ?? null!,
                Status = t.Status ?? null!,
                PaidDate = t.PaidDate
            }).ToList();

            return report;
        }

        public async Task<ClassFinancialDetailDto> GetClassFinancialDetailAsync(Guid classId)
        {
            var classInfo = await financialRepository.GetClassInfoAsync(classId);
            if (classInfo == null) throw new Exception("Class not found.");

            var invoices = await financialRepository.GetInvoicesByClassIdAsync(classId);

            var detailDto = new ClassFinancialDetailDto
            {
                ClassId = classInfo.ClassId,
                ClassName = classInfo.ClassName ?? null!,
                TuitionFeePerStudent = classInfo.TuitionFee
            };

            foreach (var invoice in invoices)
            {
                var paidAmount = invoice.Transactions
                    .Where(t => t.Status == "Completed" || t.Status == "Success")
                    .Sum(t => t.AmountPaid);

                detailDto.ExpectedRevenue += invoice.Amount;
                detailDto.CollectedRevenue += paidAmount;

                detailDto.StudentInvoices.Add(new StudentInvoiceDto
                {
                    InvoiceId = invoice.InvoiceId,
                    StudentId = invoice.StudentId,
                    StudentName = invoice.Student?.StudentNavigation?.FullName ?? null!,
                    ParentName = invoice.Student?.ParentName ?? null!,
                    ParentPhone = invoice.Student?.ParentPhone ?? null!,
                    PeriodMonth = invoice.PeriodMonth,
                    PeriodYear = invoice.PeriodYear,
                    TotalAmount = invoice.Amount,
                    PaidAmount = paidAmount,
                    RemainingAmount = Math.Max(0, invoice.Amount - paidAmount),
                    Status = invoice.Status ?? null!,
                    DueDate = invoice.DueDate
                });
            }

            detailDto.StudentInvoices = detailDto.StudentInvoices
                .OrderBy(i => i.Status == "Paid" ? 1 : 0)
                .ThenBy(i => i.StudentName)
                .ToList();

            return detailDto;
        }
    }
}
