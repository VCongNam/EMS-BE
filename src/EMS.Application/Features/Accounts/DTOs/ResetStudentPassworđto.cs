using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.DTOs
{
    public class ResetStudentPasswordDto
    {
        public Guid StudentId { get; set; }
        public string NewPassword { get; set; } = string.Empty;
    }
}
