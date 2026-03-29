using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.DTOs
{
    public class SubmissionDetailDto
    {
        public Guid SubmissionID { get; set; }
        public string FileURL { get; set; }
        public DateTime SubmittedAt { get; set; }
        public decimal? Grade { get; set; }
        public string Status { get; set; }
        public List<string> Feedbacks { get; set; } = new List<string>();
    }
}
