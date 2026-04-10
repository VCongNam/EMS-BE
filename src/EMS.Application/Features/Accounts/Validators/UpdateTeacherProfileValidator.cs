using EMS.Application.Features.Accounts.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.Validators
{
    public class UpdateTeacherProfileValidator : AbstractValidator<UpdateTeacherProfileRequest>
    {
        public UpdateTeacherProfileValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().WithMessage("Tên không được để trống");
            RuleFor(x => x.PhoneNumber).Matches(@"^[0-9]{10}$").WithMessage("Số điện thoại phải có 10 chữ số");
            RuleFor(x => x.BankAccount).Matches(@"^[0-9]*$").WithMessage("Số tài khoản chỉ được chứa số");
        }
    }
}
