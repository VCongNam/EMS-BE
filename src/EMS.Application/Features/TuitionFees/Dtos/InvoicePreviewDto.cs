using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class InvoicePreviewDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentStatus { get; set; }
        public DateTime EnrollmentDate { get; set; }

        public int TotalSessionsInMonth { get; set; }
        public int AttendedSessions { get; set; }
        public int ExcusedAbsences { get; set; }
        public int UnexcusedAbsences { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }
}
