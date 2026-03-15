using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class ClassSchedule
{
    public Guid ScheduleId { get; set; }

    public Guid ClassId { get; set; }

    public List<short> DayOfWeek { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public virtual Class Class { get; set; } = null!;
}
