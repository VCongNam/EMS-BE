using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class CreateClassRequest
    {
        public Guid TeacherId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string? Room { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TuitionFee { get; set; }
    }

}
