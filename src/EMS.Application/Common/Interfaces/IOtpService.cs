using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Common.Interfaces
{
    public interface IOtpService
    {
        string GenerateOtp();
        string HashOtp(string otp);
        bool VerifyOtp(string plainOtp, string hashedOtp);
    }
}
