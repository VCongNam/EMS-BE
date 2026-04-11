using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth.DTOs
{
    public class ResendOtpRequest
    {
        public string Email { get; set; } = null!;
    }
}
