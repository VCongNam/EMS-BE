using EMS.Application.Features.Notifications.Services;
using EMS.Domain.Interfaces;

namespace EMS.API.BackgroundServices
{
    public class SessionReminderWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionReminderWorker> _logger;
        private readonly HashSet<Guid> _sentSessionIds = new HashSet<Guid>();
        public SessionReminderWorker(IServiceProvider serviceProvider, ILogger<SessionReminderWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker nhắc lịch học đang chạy lúc: {time}", DateTimeOffset.Now);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var sessionRepo = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var now = DateTime.Now;
                    var targetStart = now.AddMinutes(55);
                    var targetEnd = now.AddMinutes(65);

                    var upcomingSessions = await sessionRepo.GetUpcomingSessionsAsync(targetStart, targetEnd);

                    foreach (var session in upcomingSessions)
                    {
                        if (!_sentSessionIds.Contains(session.SessionId))
                        {
                            try
                            {
                                var targets = await notificationService.GetAllClassTargetsAsync(session.ClassId);
                                if (targets.Any())
                                {
                                    await notificationService.SendBulkNotificationWithStudentAsync(
                                        targets: targets,
                                        title: "Nhắc lịch học sắp tới",
                                        content: $"Bạn có buổi học '{session.Title}' của lớp {session.Class.ClassName} bắt đầu vào lúc {session.StartTime:hh\\:mm}. Bạn lưu ý tham gia đúng giờ!",
                                        actionUrl: $"/schedule",
                                        type: "Reminder"
                                    );
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Lỗi nhắc lịch Session {session.SessionId}: {ex.Message}");
                            }
                            _sentSessionIds.Add(session.SessionId);
                        }
                    }
                    if (now.Hour == 0 && now.Minute < 15) _sentSessionIds.Clear();
                }
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
