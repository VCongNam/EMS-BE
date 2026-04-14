using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class PostpaidStudentInvoiceDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public Guid? InvoiceId { get; set; }
        public int AttendedSessions { get; set; } // Số buổi đã hiện diện
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
