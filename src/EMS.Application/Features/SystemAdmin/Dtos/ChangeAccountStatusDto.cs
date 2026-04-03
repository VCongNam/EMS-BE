using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.SystemAdmin.Dtos
{
    public class ChangeAccountStatusDto
    {
        public string NewStatus { get; set; } = null!;
        public string Reason { get; set; } = null!; // Lý do gửi qua email
    }
}
