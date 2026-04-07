using EMS.Application.Features.TuitionFees.Dtos;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Validators
{
    public class GenerateInvoiceValidator : AbstractValidator<GenerateInvoiceDto>
    {
        public GenerateInvoiceValidator()
        {
            RuleFor(x => x.PeriodMonth).InclusiveBetween(1, 12).WithMessage("Tháng không hợp lệ.");
            RuleFor(x => x.PeriodYear).GreaterThanOrEqualTo(DateTime.UtcNow.Year).WithMessage("Năm không hợp lệ.");
            RuleFor(x => x.DueDate).GreaterThan(DateTime.UtcNow).WithMessage("Hạn nộp phải sau ngày hiện tại.");
        }
    }
}
