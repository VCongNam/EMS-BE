using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.DTOs
{
    public class TuitionInvoiceDetailDto
    {
        public Guid InvoiceID { get; set; }
        public string Title { get; set; }
        public string Period { get; set; }
        public DateTime DueDate { get; set; }

        public int TotalSessions { get; set; } // Số buổi học tính tiền
        public decimal UnitPrice { get; set; } // Đơn giá 
        public decimal TotalAmount { get; set; } // Tổng tiền

        public string StatusDisplay { get; set; }
        public bool CanPay { get; set; }

    }

    public class BilledSessionDto
    {
        public DateOnly Date { get; set; }
        public string Title { get; set; } // VD: "Buổi 1: Luyện đề"
        public string AttendanceStatus { get; set; } // "Có mặt" hoặc "Vắng có phép"
    }
}
