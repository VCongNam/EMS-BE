using EMS.Application.Common.Interfaces;
using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Services
{
    public class EmailQueue : IEmailQueue
    {
        private readonly Channel<EmailMessage> queue;

        public EmailQueue()
        {
            var options = new UnboundedChannelOptions { SingleReader = true };
            queue = Channel.CreateUnbounded<EmailMessage>(options);
        }

        public async ValueTask QueueEmailAsync(EmailMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            await queue.Writer.WriteAsync(message);
        }

        public async ValueTask<EmailMessage> DequeueEmailAsync(CancellationToken cancellationToken)
        {
            return await queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
