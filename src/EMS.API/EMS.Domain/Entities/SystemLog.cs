using System;
using System.Collections.Generic;

namespace EMS.API.EMS.Domain.Entities;

public partial class SystemLog
{
    public Guid LogId { get; set; }

    public Guid? AccountId { get; set; }

    public string ActionType { get; set; } = null!;

    public string TableName { get; set; } = null!;

    public Guid RecordId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? Ipaddress { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Account? Account { get; set; }
}
