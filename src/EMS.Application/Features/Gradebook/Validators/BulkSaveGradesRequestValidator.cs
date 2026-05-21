using EMS.Application.Features.Gradebook.DTOs;
using FluentValidation;

namespace EMS.Application.Features.Gradebook.Validators
{
    public class BulkSaveGradesRequestValidator : AbstractValidator<BulkSaveGradesRequest>
    {
        public BulkSaveGradesRequestValidator()
        {
         
        }
    }

    public class GradeCellDtoValidator : AbstractValidator<GradeCellDto>
    {
        public GradeCellDtoValidator()
        {
            RuleFor(x => x.AssignmentId)
                .NotEmpty().WithMessage("Mã bài tập là bắt buộc.");

            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("Mã học sinh là bắt buộc.");

            RuleFor(x => x.Grade)
                .InclusiveBetween(0, 10).WithMessage("Điểm phải nằm trong khoảng từ 0 đến 10.")
                .When(x => x.Grade.HasValue);
        }
    }
}
