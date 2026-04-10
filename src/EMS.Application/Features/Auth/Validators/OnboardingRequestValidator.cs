using EMS.Application.Features.Auth.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth.Validators
{
    public class OnboardingRequestValidator : AbstractValidator<OnboardingRequest>
    {
        public OnboardingRequestValidator()
        {
            RuleFor(x => x.OldPassword)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu hiện tại");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu mới")
                .MinimumLength(8).WithMessage("Mật khẩu mới phải có ít nhất 8 ký tự")
                .Matches(@"[A-Z]").WithMessage("Mật khẩu phải có ít nhất 1 chữ cái viết hoa")
                .Matches(@"[a-z]").WithMessage("Mật khẩu phải có ít nhất 1 chữ cái viết thường")
                .Matches(@"[0-9]").WithMessage("Mật khẩu phải có ít nhất 1 chữ số");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword).WithMessage("Mật khẩu xác nhận không khớp");
        }
    }
}
