using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EMS.Application.Features.Assignments.DTOs
{
    public class GradeSubmissionDto
    {
        [Required]
        [Range(0, 10)]
        public decimal Grade { get; set; }

        public List<IFormFile> CorrectionFiles { get; set; } = new();
    }
}
