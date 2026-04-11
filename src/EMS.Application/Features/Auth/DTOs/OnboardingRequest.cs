using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth.DTOs
{
    public class OnboardingRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string OldPassword { get; set; } = string.Empty; 
        public string NewPassword { get; set; } = string.Empty; 
        public string ConfirmPassword { get; set; } = string.Empty; 
    }
}
