using EMS.Application.Features.TuitionFees.Dtos;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Validators
{
    public class ExtendInvoiceValidator : AbstractValidator<ExtendClassInvoicesDto>
    {
        public ExtendInvoiceValidator() 
        {
            RuleFor(x => x.PeriodMonth)
                .InclusiveBetween(1, 12).WithMessage("Tháng phải nằm trong khoảng từ 1 đến 12.");
            RuleFor(x => x.PeriodYear).GreaterThanOrEqualTo(DateTime.Now.Year).WithMessage("Năm phải lớn hơn hoặc bằng năm hiện tại.");
            RuleFor(x => x.AdditionalDays)
                .GreaterThan(0).WithMessage("Số ngày gia hạn phải lớn hơn 0.")
                .LessThanOrEqualTo(7).WithMessage("Số ngày gia hạn không được vượt quá 7 ngày.");
        }
    }
}
