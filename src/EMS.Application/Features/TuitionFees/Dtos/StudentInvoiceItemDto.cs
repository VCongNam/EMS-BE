using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class StudentInvoiceItemDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public int? AttendedSessions { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal CreditBalance { get; set; } // Thêm để show ví cấn trừ
        public string Status { get; set; } = null!;
    }
}
