using System.ComponentModel.DataAnnotations;

namespace EMS.Application.Features.Assignments.DTOs
{
    public class FeedbackSubmissionDto
    {
        [Required]
        public string Content { get; set; } = null!;
    }
}
