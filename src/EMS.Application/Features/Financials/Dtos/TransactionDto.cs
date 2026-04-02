using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Financials.Dtos
{
    public class TransactionDto
    {
        public Guid TransactionId { get; set; }
        public string StudentName { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? PaidDate { get; set; }
    }
}
