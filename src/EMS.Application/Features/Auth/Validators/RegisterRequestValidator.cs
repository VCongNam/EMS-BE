using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMS.Application.Features.Auth.DTOs;

namespace EMS.Application.Features.Auth.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Lỗi định dạng email.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.")
                .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
                .Matches(@"[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự in hoa.")
                .Matches(@"[a-z]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự in thường.")
                .Matches(@"[0-9]").WithMessage("Mật khẩu phải chứa ít nhất một chữ số.")
                .Matches(@"[\!\?\*\.\@]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự đặc biệt (!?*.@).");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ và tên không được để trống.")
                .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");

            RuleFor(x => x.RoleName)
                .Must(role => role == "Teacher" || role == "TA")
                .WithMessage("Quyền đăng ký không hợp lệ. Chỉ được chọn Giáo viên hoặc Trợ giảng.");
        }
    }
}
