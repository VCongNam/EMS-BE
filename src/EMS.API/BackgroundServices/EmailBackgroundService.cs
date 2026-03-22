using Microsoft.Extensions.Hosting; // <-- Dòng quan trọng bạn đang thiếu
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EMS.Application.Common.Interfaces;
using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.API.BackgroundServices
{
    public class EmailBackgroundService : BackgroundService
    {
        private readonly IEmailQueue emailQueue;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<EmailBackgroundService> logger;

        public EmailBackgroundService(IEmailQueue emailQueue, IServiceProvider serviceProvider, ILogger<EmailBackgroundService> logger)
        {
            this.emailQueue = emailQueue;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Email Background Service đang chạy...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Lấy email từ hàng đợi (nếu không có, nó sẽ nằm chờ ở đây)
                    var emailMessage = await emailQueue.DequeueEmailAsync(stoppingToken);

                    // Mở một Scope mới để lấy IEmailService (vì BackgroundService là Singleton)
                    using var scope = serviceProvider.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    await emailService.SendEmailAsync(emailMessage.To, emailMessage.Subject, emailMessage.Body);
                }
                catch (OperationCanceledException)
                {
                    // Bỏ qua lỗi khi tắt server
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Lỗi khi xử lý hàng đợi email");
                }
            }
        }
    }
}