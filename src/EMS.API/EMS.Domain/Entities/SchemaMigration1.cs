using System;
using System.Collections.Generic;

namespace EMS.API.EMS.Domain.Entities;

public partial class SchemaMigration1
{
    public long Version { get; set; }

    public DateTime? InsertedAt { get; set; }
}
