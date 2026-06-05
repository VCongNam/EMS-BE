using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public class GlobalInvoiceRecordDto
    {
        public Guid InvoiceId { get; set; }
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string BillingMethod { get; set; } = string.Empty;

        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public int SessionCount { get; set; }

        public decimal TotalAmount { get; set; }    

        public decimal PaidAmount { get; set; }  
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = string.Empty;

        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
    }
}
