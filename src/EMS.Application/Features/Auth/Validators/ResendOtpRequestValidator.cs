using EMS.Application.Features.Auth.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth.Validators
{
    public class ResendOtpRequestValidator : AbstractValidator<ResendOtpRequest>
    {
        public ResendOtpRequestValidator() 
        {
            RuleFor(x => x.Email)
                   .NotEmpty().WithMessage("Email không được để trống.")
                   .EmailAddress().WithMessage("Lỗi định dạng email.");
        }
    }
}
