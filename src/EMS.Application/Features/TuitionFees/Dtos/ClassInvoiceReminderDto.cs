using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class ClassInvoiceReminderDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string BillingMethod { get; set; } = string.Empty;
        public string TargetPeriod { get; set; } = string.Empty; // Ví dụ: "04/2026"
        public string Priority { get; set; } = string.Empty; // High, Medium
        public string Message { get; set; } = string.Empty;
    }
}
