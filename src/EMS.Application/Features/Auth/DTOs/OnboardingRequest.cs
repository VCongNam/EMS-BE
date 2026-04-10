using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth.DTOs
{
    public class OnboardingRequest
    {
        public string OldPassword { get; set; } = string.Empty; // Mật khẩu mặc định giáo viên cấp
        public string NewPassword { get; set; } = string.Empty; // Mật khẩu riêng của học sinh
        public string ConfirmPassword { get; set; } = string.Empty; // FE check trùng, BE có thể check lại cho chắc
    }
}
