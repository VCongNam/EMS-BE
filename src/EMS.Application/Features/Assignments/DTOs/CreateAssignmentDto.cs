using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assignments.DTOs
{
    public class CreateAssignmentDto
    {
        public Guid ClassId { get; set; }
        public Guid AuthorId { get; set; }
        public Guid GradeCategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AttachmentPath { get; set; }
        public DateTime DueDate { get; set; }
    }

}
