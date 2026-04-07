using EMS.Application.Features.TuitionFees.Dtos;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Validators
{
    public class UpdateTuitionFeeValidator : AbstractValidator<UpdateTuitionFeeDto>
    {
        public UpdateTuitionFeeValidator()
        {
            RuleFor(x => x.TuitionFee)
                .GreaterThan(10000).WithMessage("Học phí mỗi buổi phải lớn hơn 10.000đ.")
                .NotEmpty().WithMessage("Vui lòng không để trống học phí.");

            RuleFor(x => x.BillingMethod)
                .Must(x => x == "Prepaid" || x == "Postpaid")
                .WithMessage("Hình thức thu phí phải là 'Prepaid' hoặc 'Postpaid'.");
        }
    }
}
