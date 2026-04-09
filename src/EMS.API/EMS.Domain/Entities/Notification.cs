using System;
using System.Collections.Generic;

namespace EMS.API.EMS.Domain.Entities;

public partial class Notification
{
    public Guid NotificationId { get; set; }

    public Guid AccountId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Type { get; set; }

    public string? ActionUrl { get; set; }

    public virtual Account Account { get; set; } = null!;
}
