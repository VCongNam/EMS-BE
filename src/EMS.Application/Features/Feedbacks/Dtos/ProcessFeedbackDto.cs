using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Feedbacks.Dtos
{
    public class ProcessFeedbackDto
    {
        public string NewStatus { get; set; } = null!;
        public string? AdminReply { get; set; }
    }
}
