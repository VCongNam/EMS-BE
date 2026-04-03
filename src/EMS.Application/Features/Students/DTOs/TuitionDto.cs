using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.DTOs
{
    public class TuitionDto
    {
        public Guid InvoiceId { get; set; }
        public Guid ClassId { get; set; }
        public string ClassName { get; set; }
        public string Period {  get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public string DisplayStatus { get; set; }
        public bool CanPay { get; set; }
    }

    public class TuitionFilter
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;

        // Null = Xem tổng học phí tất cả các lớp
        // Có giá trị = Xem học phí của riêng lớp đó
        public Guid? ClassID { get; set; }
    }
}
