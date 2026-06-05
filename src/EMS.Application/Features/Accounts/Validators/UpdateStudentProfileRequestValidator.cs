using EMS.Application.Features.Accounts.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.Validators
{
    public class UpdateStudentProfileRequestValidator : AbstractValidator<UpdateStudentProfileRequest>
    {
        public UpdateStudentProfileRequestValidator()
        {
            

            RuleFor(x => x.Dob)
                .NotEmpty().WithMessage("Ngày sinh không được để trống")
                .Must(dob => dob.ToDateTime(TimeOnly.MinValue) < DateTime.Now)
                .WithMessage("Ngày sinh không thể ở tương lai");

            RuleFor(x => x.Address)
                .MaximumLength(255).WithMessage("Địa chỉ quá dài");
        }
    }
}
