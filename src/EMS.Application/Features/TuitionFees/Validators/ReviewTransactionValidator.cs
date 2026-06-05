using EMS.Application.Features.TuitionFees.Dtos;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Validators
{
    public class ReviewTransactionValidator : AbstractValidator<ReviewTransactionDto>
    {
        public ReviewTransactionValidator()
        {
            RuleFor(x => x.IsApproved)
                .NotNull().WithMessage("Trạng thái duyệt không được để trống.");

            RuleFor(x => x.Note)
                .NotEmpty()
                .When(x => x.IsApproved == false)
                .WithMessage("Vui lòng nhập lý do từ chối minh chứng này để phụ huynh nắm được.");

            RuleFor(x => x.Note)
                .MaximumLength(500)
                .WithMessage("Ghi chú không được vượt quá 500 ký tự.");
        }
    }
}
