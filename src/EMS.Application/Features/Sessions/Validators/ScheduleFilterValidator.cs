using EMS.Application.Features.Sessions.DTOs;
using FluentValidation;

namespace EMS.Application.Features.Sessions.Validators
{
    public class ScheduleFilterValidator : AbstractValidator<ScheduleFilter>
    {
        public ScheduleFilterValidator()
        {
            RuleFor(x => x)
                .Must(x => x.FromDate <= x.ToDate)
                .WithMessage("Ngày bắt đầu phải trước ngày kết thúc.");
        }
    }
}
