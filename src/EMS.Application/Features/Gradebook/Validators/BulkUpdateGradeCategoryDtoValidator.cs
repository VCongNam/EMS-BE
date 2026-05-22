using EMS.Application.Features.Gradebook.DTOs;
using FluentValidation;

namespace EMS.Application.Features.Gradebook.Validators
{
    public class BulkUpdateGradeCategoryDtoValidator : AbstractValidator<BulkUpdateGradeCategoryDto>
    {
        public BulkUpdateGradeCategoryDtoValidator()
        {
           
            RuleForEach(x => x.Categories)
                .SetValidator(new UpdateGradeCategoryDtoValidator());
        }
    }
}
