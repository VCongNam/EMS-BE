using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Gradebook.DTOs
{
    public class BulkSaveGradesRequest
    {        public List<GradeCellDto> ChangedGrades { get; set; } = new();
    }

    public class GradeCellDto
    {
        public Guid AssignmentId { get; set; }
        public Guid StudentId { get; set; }
        public decimal? Grade { get; set; }
    }

}
