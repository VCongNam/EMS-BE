using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Common.Exceptions
{
    public class ConflictException : Exception
    {
        public ConflictException() : base("Dữ liệu bị xung đột hoặc đã tồn tại trong hệ thống.") { }

        public ConflictException(string message) : base(message) { }

        public ConflictException(string name, object key)
            : base($"Bản ghi '{name}' với thông tin ({key}) đã tồn tại trong hệ thống. Vui lòng kiểm tra lại.") { }
    }
}
