using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth.DTOs
{
    public class LoginRequest
    {
        public string Identifier { get; set; } = string.Empty; // SĐT hoặc Email
        public string Password { get; set; } = string.Empty;
        public string SelectedRole { get; set; } = string.Empty; // "Admin", "Teacher", "TA", "Student"
    }
}
