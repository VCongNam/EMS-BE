using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Common.Interfaces
{
    public interface IEmailQueue
    {
        ValueTask QueueEmailAsync(EmailMessage message);
        ValueTask<EmailMessage> DequeueEmailAsync(CancellationToken cancellationToken);
    }
}
