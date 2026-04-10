using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using EMS.Application.Features.Auth.DTOs;

namespace EMS.Application.Features.Auth.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Identifier)
                .NotEmpty().WithMessage("Tài khoản không được để trống");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống")
                .MinimumLength(6).WithMessage("Mật khẩu phải từ 6 ký tự trở lên");

            RuleFor(x => x.SelectedRole)
                .NotEmpty().WithMessage("Vui lòng chọn vai trò đăng nhập")
                .Must(role => new[] { "Admin", "Teacher", "TA", "Student" }.Contains(role))
                .WithMessage("Vai trò không hợp lệ");
        }
    }
}
