using System.ComponentModel.DataAnnotations;

namespace EMS.Application.Features.Assignments.DTOs
{
    public class GradeSubmissionDto
    {
        [Required]
        [Range(0, 10)]
        public decimal Grade { get; set; }
    }
}
