using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class PayCashDto
    {
        public decimal Amount { get; set; }
        public string? Note { get; set; } = string.Empty;
    }
}
