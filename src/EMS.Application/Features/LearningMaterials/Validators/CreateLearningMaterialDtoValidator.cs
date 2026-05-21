using EMS.Application.Features.LearningMaterials.DTOs;
using FluentValidation;

namespace EMS.Application.Features.LearningMaterials.Validators
{
    public class CreateLearningMaterialDtoValidator : AbstractValidator<CreateLearningMaterialDto>
    {
        public CreateLearningMaterialDtoValidator()
        {
            RuleFor(x => x.ClassId)
                .NotEmpty().WithMessage("Mã lớp học là bắt buộc.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề tài liệu không được để trống.")
                .MaximumLength(200).WithMessage("Tiêu đề tài liệu không được vượt quá 200 ký tự.");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Mô tả không được vượt quá 2000 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            RuleForEach(x => x.Attachments!)
                .ApplyFileRules()
                .When(x => x.Attachments != null && x.Attachments.Count > 0);
        }
    }
}
