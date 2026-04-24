using EMS.Application.Features.Accounts.DTOs;
using FluentValidation;
using FluentValidation.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.Validators
{
    public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordRequestValidator() 
        { 
            RuleFor(x => x.OldPassword)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu hiện tại");
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu mới")
                .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
                .Matches(@"[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự in hoa.")
                .Matches(@"[a-z]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự in thường.")
                .Matches(@"[0-9]").WithMessage("Mật khẩu phải chứa ít nhất một chữ số.")
                .Matches(@"[\!\?\*\.\@]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự đặc biệt (!?*.@).");
        }
    }
}
