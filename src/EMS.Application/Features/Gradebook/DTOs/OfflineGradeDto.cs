using System;
using System.ComponentModel.DataAnnotations;

namespace EMS.Application.Features.Gradebook.DTOs
{
    public class OfflineGradeDto
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        [Range(0, 10)]
        public decimal Grade { get; set; }
    }
}
