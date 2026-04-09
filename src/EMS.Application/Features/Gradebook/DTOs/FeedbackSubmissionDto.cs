using System.ComponentModel.DataAnnotations;

namespace EMS.Application.Features.Gradebook.DTOs
{
    public class FeedbackSubmissionDto
    {
        [Required]
        public string Content { get; set; } = null!;
    }
}
