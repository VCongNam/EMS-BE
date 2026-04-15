using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class DailyRevenueDto
    {
        public int Day { get; set; }
        public decimal ReceivedAmount { get; set; } // Tiền thu được trong ngày này
    }
}
