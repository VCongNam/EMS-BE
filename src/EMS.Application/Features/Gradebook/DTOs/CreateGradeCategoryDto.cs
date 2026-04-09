using System;
using System.ComponentModel.DataAnnotations;

namespace EMS.Application.Features.Gradebook.DTOs
{
    public class CreateGradeCategoryDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [Range(0, 100)]
        public decimal Weight { get; set; }
    }
}
