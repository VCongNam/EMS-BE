using EMS.Application.Features.LearningMaterials.DTOs;
using FluentValidation;

namespace EMS.Application.Features.LearningMaterials.Validators
{
    public class UpdateLearningMaterialDtoValidator : AbstractValidator<UpdateLearningMaterialDto>
    {
        public UpdateLearningMaterialDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề tài liệu không được để trống.")
                .MaximumLength(200).WithMessage("Tiêu đề tài liệu không được vượt quá 200 ký tự.");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Mô tả không được vượt quá 2000 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            RuleForEach(x => x.NewAttachments!)
                .ApplyFileRules()
                .When(x => x.NewAttachments != null && x.NewAttachments.Count > 0);

            RuleForEach(x => x.RemoveAttachmentIds!)
                .NotEmpty().WithMessage("Mã file đính kèm cần xóa không hợp lệ.")
                .When(x => x.RemoveAttachmentIds != null && x.RemoveAttachmentIds.Count > 0);
        }
    }
}
