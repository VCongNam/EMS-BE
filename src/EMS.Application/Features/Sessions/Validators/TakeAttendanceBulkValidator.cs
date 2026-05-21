using EMS.Application.Features.Sessions.DTOs;
using FluentValidation;

namespace EMS.Application.Features.Sessions.Validators
{
    public class TakeAttendanceBulkValidator : AbstractValidator<List<TakeAttendanceDto>>
    {
        public TakeAttendanceBulkValidator()
        {
            RuleFor(x => x)
                .NotNull().WithMessage("Danh sách điểm danh không được để trống.")
                .NotEmpty().WithMessage("Danh sách điểm danh không được để trống.");

            RuleForEach(x => x)
                .SetValidator(new TakeAttendanceDtoValidator());
        }
    }
}
