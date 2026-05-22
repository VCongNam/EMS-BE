using EMS.Application.Features.Sessions.DTOs;
using FluentValidation;

namespace EMS.Application.Features.Sessions.Validators
{
    public class CreateSessionDtoValidator : AbstractValidator<CreateSessionDto>
    {
        public CreateSessionDtoValidator()
        {
            RuleFor(x => x.ClassId)
                .NotEmpty().WithMessage("Mã lớp học là bắt buộc.");

            RuleFor(x => x.Title)
                .MaximumLength(200).WithMessage("Tiêu đề buổi học không được vượt quá 200 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Title));

            RuleFor(x => x.Topic)
                .MaximumLength(500).WithMessage("Chủ đề không được vượt quá 500 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Topic));

            RuleFor(x => x.Note)
                .MaximumLength(1000).WithMessage("Ghi chú không được vượt quá 1000 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Note));

            RuleFor(x => x.MeetingLink)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .WithMessage("Link cuộc họp không hợp lệ.")
                .When(x => !string.IsNullOrWhiteSpace(x.MeetingLink));

            RuleFor(x => x)
                .Must(x => !x.StartTime.HasValue || !x.EndTime.HasValue || x.StartTime < x.EndTime)
                .WithMessage("Thời gian bắt đầu phải trước thời gian kết thúc.");
        }
    }
}
