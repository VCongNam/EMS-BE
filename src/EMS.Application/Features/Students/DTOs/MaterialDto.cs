using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.DTOs
{
    public class MaterialDto
    {
        public Guid MaterialID { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string FileURL { get; set; } 
        public string FileType { get; set; } 
        public DateTime CreatedAt { get; set; }
    }
}
