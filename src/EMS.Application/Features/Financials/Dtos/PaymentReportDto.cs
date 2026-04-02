using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Financials.Dtos
{
    public class PaymentReportDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public int TotalPaidInvoices { get; set; }
        public int TotalPendingInvoices { get; set; }
        public List<TransactionDto> RecentTransactions { get; set; } = new();
    }
}
