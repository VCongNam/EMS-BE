using EMS.Application.Features.Gradebook.DTOs;
using FluentValidation;

namespace EMS.Application.Features.Gradebook.Validators
{
    public class CreateGradeCategoryDtoValidator : AbstractValidator<CreateGradeCategoryDto>
    {
        public CreateGradeCategoryDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên đầu điểm không được để trống.")
                .MaximumLength(100).WithMessage("Tên đầu điểm không được vượt quá 100 ký tự.");

            RuleFor(x => x.Weight)
                .InclusiveBetween(0, 100).WithMessage("Trọng số đầu điểm phải nằm trong khoảng từ 0 đến 100.");
        }
    }
}
