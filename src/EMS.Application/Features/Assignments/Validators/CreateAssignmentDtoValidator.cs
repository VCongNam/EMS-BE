using EMS.Application.Features.Assignments.DTOs;
using FluentValidation;

namespace EMS.Application.Features.Assignments.Validators
{
    public class CreateAssignmentDtoValidator : AbstractValidator<CreateAssignmentDto>
    {
        public CreateAssignmentDtoValidator()
        {
            RuleFor(x => x.GradeCategoryId)
                .NotNull().WithMessage("Grade category là bắt buộc khi bài tập được chấm điểm.")
                .NotEqual(Guid.Empty).WithMessage("Grade category là bắt buộc khi bài tập được chấm điểm.")
                .When(x => x.Isgraded);
        }
    }
}
