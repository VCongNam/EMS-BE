using System.ComponentModel.DataAnnotations;

namespace EMS.Application.Features.Gradebook.DTOs
{
    public class GradeSubmissionDto
    {
        [Required]
        [Range(0, 10)] // The user requested the grading scale to be 10.
        public decimal Grade { get; set; }
    }
}
