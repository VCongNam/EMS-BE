using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.DTOs
{
    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }



    }
}
