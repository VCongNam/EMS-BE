using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Common.Interfaces
{
    public interface ICaptchaService
    {
        Task<bool> VerifyCaptchaAsync(string token);
    }
}
