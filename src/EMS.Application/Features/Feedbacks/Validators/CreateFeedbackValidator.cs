using EMS.Application.Features.Feedbacks.Dtos;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Feedbacks.Validators
{
    public class CreateFeedbackValidator : AbstractValidator<CreateFeedbackDto>
    {
        public CreateFeedbackValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Content).NotEmpty().MinimumLength(10);
            RuleFor(x => x.Type).Must(t => new[] { "Bug", "FeatureRequest", "Inquiry", "General" }.Contains(t));
        }
    }
}
