using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.DTOs
{
    public class UpdateTAProfileRequest
    {
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Bio { get; set; }
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }
        public string? BankAccountName { get; set; }
    }
}
