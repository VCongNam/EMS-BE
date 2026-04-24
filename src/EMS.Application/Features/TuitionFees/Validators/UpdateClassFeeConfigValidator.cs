using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMS.Application.Features.TuitionFees.Dtos;

namespace EMS.Application.Features.TuitionFees.Validators
{
    public class UpdateClassFeeConfigValidator : AbstractValidator<UpdateClassFeeConfigDto>
    {
        public UpdateClassFeeConfigValidator() 
        {
            RuleFor(x => x.PaymentDeadlineDays)
                .GreaterThanOrEqualTo(0).WithMessage("Số ngày thanh toán phải lớn hơn 0.")
                .LessThanOrEqualTo(7).WithMessage("Số ngày thanh toán không được vượt quá 7 ngày.");
            RuleFor(x => x.TuitionFee)
                .GreaterThan(0).WithMessage("Học phí phải lớn hơn 0.");
            RuleFor(x => x.BillingMethod)
                .NotEmpty().WithMessage("Phương thức thanh toán không được để trống.")
                .Must(method => method == "Postpaid")
                .WithMessage("Hệ thống chỉ hỗ trợ phương thức thanh toán 'Thu sau'.");

        }
    }
}
