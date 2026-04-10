using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.DTOs
{
    public class TransactionViewDto
    {
        public Guid TransactionId { get; set; }

        public string InvoiceContent { get; set; }

        public decimal AmountPaid { get; set; }
        public DateTime? PaidDate { get; set; }
        public string Status { get; set; }
    }

    public class TransactionDetailDto
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
