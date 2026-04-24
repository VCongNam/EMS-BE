using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class ConfirmInvoiceItemDto
    {
        public Guid StudentId { get; set; }
        public int AttendedSessions { get; set; }
    }
}
