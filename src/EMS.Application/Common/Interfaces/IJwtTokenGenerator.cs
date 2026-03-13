using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(Account account, string roleName);
    }
}
