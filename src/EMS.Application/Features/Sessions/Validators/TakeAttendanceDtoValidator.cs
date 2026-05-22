using EMS.Application.Features.Sessions.DTOs;
using FluentValidation;

namespace EMS.Application.Features.Sessions.Validators
{
    public class TakeAttendanceDtoValidator : AbstractValidator<TakeAttendanceDto>
    {
        private static readonly string[] AllowedStatuses = { "Present", "Absent" };

        public TakeAttendanceDtoValidator()
        {
            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("Mã học sinh là bắt buộc.");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Trạng thái điểm danh là bắt buộc.")
                .Must(status => AllowedStatuses.Contains(status))
                .WithMessage("Trạng thái điểm danh chỉ hỗ trợ 'Present' hoặc 'Absent'.");

            RuleFor(x => x.IsExcused)
                .NotNull().WithMessage("Vui lòng xác định vắng có phép hay không.")
                .When(x => x.Status == "Absent");

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Note));
        }
    }
}
