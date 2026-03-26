using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.ProgressReports.DTOs
{
    public class CreateProgressReportDto
    {
        public Guid StudentId { get; set; }
        public Guid ClassId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
    }
}
