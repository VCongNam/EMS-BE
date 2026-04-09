using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class ExtendClassInvoicesDto
    {
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public int AdditionalDays { get; set; }
    }
}
