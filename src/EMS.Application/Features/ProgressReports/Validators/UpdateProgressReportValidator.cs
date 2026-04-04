using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMS.Application.Features.ProgressReports.DTOs;
using FluentValidation;

namespace EMS.Application.Features.ProgressReports.Validators
{
    public class UpdateProgressReportValidator : AbstractValidator<UpdateProgressReportDto>
    {
        public UpdateProgressReportValidator()
        {
            RuleFor(x => x.Content).NotEmpty().MinimumLength(10).WithMessage("Nhận xét tối thiểu 10 ký tự.");
            RuleFor(x => x.Status).Must(s => s == "Draft" || s == "Published").WithMessage("Trạng thái không hợp lệ.");
        }
    }
}
