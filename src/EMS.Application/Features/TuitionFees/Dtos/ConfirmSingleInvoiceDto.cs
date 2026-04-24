using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class ConfirmSingleInvoiceDto
    {
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public DateTime DueDate { get; set; }
        public int AttendedSessions { get; set; }
    }
}
