using System.Collections.Generic;

namespace EMS.Application.Features.Gradebook.DTOs
{
    public class BulkUpdateGradeCategoryDto
    {
        public List<UpdateGradeCategoryDto> Categories { get; set; } = new List<UpdateGradeCategoryDto>();
    }
}
