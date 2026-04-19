using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class ClassDetailDto
    {
        public Guid ClassId { get; set; }
        public Guid TeacherId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string? Room { get; set; }
        public short? MaxStudents { get; set; }
        public int CurrentStudents { get; set; }
        public decimal TuitionFee { get; set; }
        public string BillingMethod { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public int PaymentDeadlineDays { get; set; }
        public string? TuitionNote { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ScheduleDto> Schedules { get; set; } = new();
    }

}
