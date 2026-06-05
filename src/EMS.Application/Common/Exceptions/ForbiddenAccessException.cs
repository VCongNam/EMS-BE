using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Common.Exceptions
{
    public class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException() : base("Bạn không có quyền thực hiện thao tác này.") { }

        public ForbiddenAccessException(string message) : base(message) { }
    }
}
