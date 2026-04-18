using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class StudentTransactionViewDto
    {
        public Guid TransactionId { get; set; }
        public Guid InvoiceId { get; set; }

        public string InvoiceContent { get; set; }

        public decimal AmountPaid { get; set; }
        public DateTime? PaidDate { get; set; }
        public string Status { get; set; }
    }

    public class StudentTransactionDetailDto
    {
        public Guid TransactionId { get; set; }
        public string InvoiceContent { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; } 
        public string? ProofImageURL { get; set; }
        public DateTime? PaidDate { get; set; }
        public string Status { get; set; }
    }
}
