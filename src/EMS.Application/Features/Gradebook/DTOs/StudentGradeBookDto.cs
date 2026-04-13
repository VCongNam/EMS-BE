using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Gradebook.DTOs
{
    public class StudentGradeBookDto
    {
        public Guid ClassId { get; set; }
        public Guid StudentId { get; set; }
        public decimal CurrentAverageScore { get; set; }
        public List<CategoryGradeDto> GradeReportTable { get; set; } = new();
    }

    public class CategoryGradeDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Weight { get; set; } 
        public decimal? CategoryScore { get; set; } 
        public List<AssignmentGradeItemDto> Assignments { get; set; } = new();
    }

    public class AssignmentGradeItemDto
    {
        public Guid AssignmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal? Score { get; set; } 
                                           
        public string CommentFeedback { get; set; } = string.Empty;
    }
}
