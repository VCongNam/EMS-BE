using System;
using System.Collections.Generic;

namespace EMS.API.EMS.Domain.Entities;

/// <summary>
/// Auth: Manages updates to the auth system.
/// </summary>
public partial class SchemaMigration
{
    public string Version { get; set; } = null!;
}
