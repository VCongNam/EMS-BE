using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth.DTOs
{
    public class StudentProfileDto
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
    }
}
