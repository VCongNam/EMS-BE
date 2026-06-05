using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class CreateClassDto
    {
        public string ClassName { get; set; } = string.Empty;
        public string? Room { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal TuitionFee { get; set; }
        public short? MaxStudents { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public short? GradeLevel { get; set; }
        public string? BillingMethod { get; set; }
        public string? BillingCycle { get; set; } = "Monthly";
        public int? PaymentDeadlineDays { get; set; }
        public string? TuitionNote { get; set; }
        public List<ScheduleDto> Schedules { get; set; } = new();
    }

    public class ScheduleDto
    {
        public short DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }

}
