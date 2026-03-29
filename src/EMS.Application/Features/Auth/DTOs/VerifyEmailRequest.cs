using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth.DTOs
{
    public class VerifyEmailRequest
    {
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }
}
