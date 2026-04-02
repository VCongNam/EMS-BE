using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Financials.Dtos
{
    public class StudentInvoiceDto
    {
        public Guid InvoiceId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public string ParentName { get; set; } = null!;
        public string ParentPhone { get; set; } = null!;
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime DueDate { get; set; }
    }
}
