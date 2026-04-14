using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class UnifiedStudentInvoiceDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public Guid? InvoiceId { get; set; }

        // DÙNG CHUNG: Lớp Thu sau thì đây là Số buổi ĐÃ HỌC, Lớp Thu trước thì đây là Số buổi DỰ KIẾN
        public int SessionCount { get; set; }

        // TRƯỜNG DÙNG RIÊNG: Lớp Thu sau luôn bằng 0, Lớp Thu trước sẽ có số tiền thực tế
        public decimal CreditBalance { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
