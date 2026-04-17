using EMS.Application.Features.Classes.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.Validators
{
    public class CreateClassDtoValidator : AbstractValidator<CreateClassDto>
    {
        public CreateClassDtoValidator()
        {
            RuleFor(x => x.ClassName)
                .NotEmpty().WithMessage("Tên lớp không được để trống.")
                .MaximumLength(100).WithMessage("Tên lớp không được vượt quá 100 ký tự.");

            RuleFor(x => x.SubjectName)
                .NotEmpty().WithMessage("Tên môn học không được để trống.");
            RuleFor(x => x.GradeLevel)
                .InclusiveBetween((short)1, (short)12).WithMessage("Khối lớp phải từ 1 đến 12.");
            RuleFor(x => x.MaxStudents)
                .GreaterThan((short)0).WithMessage("Số lượng học sinh tối đa phải lớn hơn 0.");
            RuleFor(x => x.TuitionFee)
                .GreaterThanOrEqualTo(0).WithMessage("Học phí không được âm.");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Ngày bắt đầu không được để trống.");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("Ngày kết thúc không được để trống.")
                .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.")
                .Must((dto, endDate) => endDate.DayNumber - dto.StartDate.DayNumber <= 730)
                .WithMessage("Khóa học không được kéo dài quá 2 năm (730 ngày).");
            RuleForEach(x => x.Schedules).SetValidator(new ScheduleDtoValidator());
        }
    }

    public class ScheduleDtoValidator : AbstractValidator<ScheduleDto>
    {
        public ScheduleDtoValidator()
        {
            RuleFor(x => x.DayOfWeek)
                .InclusiveBetween((short)0, (short)7).WithMessage("Ngày trong tuần không hợp lệ (0-7).");

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("Giờ bắt đầu không được để trống.");

            RuleFor(x => x.EndTime)
                .NotEmpty().WithMessage("Giờ kết thúc không được để trống.")
                .GreaterThan(x => x.StartTime).WithMessage("Giờ kết thúc phải sau giờ bắt đầu.");
        }
    }

}


