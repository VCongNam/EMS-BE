using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using EMS.Application.Features.ProgressReports.DTOs;


namespace EMS.Application.Features.ProgressReports.Validators
{
    public class CreateProgressReportValidator : AbstractValidator<CreateProgressReportDto>
    {
        public CreateProgressReportValidator()
        {
            RuleFor(x => x.ClassId).NotEmpty().WithMessage("Lớp học không hợp lệ.");
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("Học sinh không hợp lệ.");
            RuleFor(x => x.PeriodMonth).InclusiveBetween(1, 12).WithMessage("Tháng phải từ 1-12.");
            RuleFor(x => x.PeriodYear).GreaterThan(2000).WithMessage("Năm không hợp lệ.");
            RuleFor(x => x.Content).NotEmpty().MinimumLength(10).WithMessage("Nhận xét tối thiểu 10 ký tự.");
            RuleFor(x => x.Status).Must(s => s == "Draft" || s == "Published").WithMessage("Trạng thái không hợp lệ.");
        }
    }
}
