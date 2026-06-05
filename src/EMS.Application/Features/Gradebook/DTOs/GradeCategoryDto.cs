using System;

namespace EMS.Application.Features.Gradebook.DTOs
{
    public class GradeCategoryDto
    {
        public Guid GradeCategoryId { get; set; }
        public Guid ClassId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Weight { get; set; }
    }
}
