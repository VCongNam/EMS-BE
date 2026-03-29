using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth.DTOs
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
