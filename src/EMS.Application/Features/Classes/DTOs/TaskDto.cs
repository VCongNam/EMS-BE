using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class TaskDto
    {
        public Guid TATaskID { get; set; }
        public string Title { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public Guid ClassID { get; set; }      
        public string ClassName { get; set; }
        public string? Feedback { get; set; } 
    }
}
