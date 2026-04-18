using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class PushSubscription
{
    public Guid SubscriptionId { get; set; }

    public Guid AccountId { get; set; }

    public string Endpoint { get; set; } = null!;

    public string P256dh { get; set; } = null!;

    public string Auth { get; set; } = null!;

    public string DeviceName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
