using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.DTOs
{
    public class EnrolledClassFilter
    {
        public int Page {  get; set; }
        public int Size { get; set; }
        public string? Status { get; set; }
    }
}
