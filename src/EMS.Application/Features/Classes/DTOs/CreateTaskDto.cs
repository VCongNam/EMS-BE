using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class CreateTaskDto
    {
        public Guid ClassTAID { get; set; }
        public string Title { get; set; }
        public DateTime DueDate { get; set; }
        public string? Type { get; set; }
    }
}
