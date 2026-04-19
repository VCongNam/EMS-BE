using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class UpdateClassDto
    {
        public string ClassName { get; set; } = string.Empty;
        public string? Room { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal TuitionFee { get; set; }
        public short? MaxStudents { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public short GradeLevel { get; set; }
        public List<ScheduleDto> Schedules { get; set; } = new();
    }

}
