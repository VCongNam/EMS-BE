using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class OverallReportDto
    {
        public decimal TotalCollected { get; set; }
        public int TotalPaidInvoices { get; set; }
        public int TotalPendingInvoices { get; set; }
    }
}
