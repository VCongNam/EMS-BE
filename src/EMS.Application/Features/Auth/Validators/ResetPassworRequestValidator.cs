using EMS.Application.Features.Auth.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth.Validators
{
    public class ResetPassworRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPassworRequestValidator() 
        {
            RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email không được để trống.")
                    .EmailAddress().WithMessage("Lỗi định dạng email.");
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Mật khẩu mới không được để trống")
                .MinimumLength(8).WithMessage("Mật khẩu mới phải từ 8 ký tự trở lên")
                .Matches(@"[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự in hoa.")
                .Matches(@"[a-z]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự in thường.")
                .Matches(@"[0-9]").WithMessage("Mật khẩu phải chứa ít nhất một chữ số.")
                .Matches(@"[\!\?\*\.\@]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự đặc biệt (!?*.@).");
        }
       
    }
}
