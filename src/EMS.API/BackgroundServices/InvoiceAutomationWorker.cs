using EMS.Application.Features.TuitionFees.Dtos;
using EMS.Application.Features.TuitionFees.Services;
using EMS.Domain.Interfaces;

namespace EMS.API.BackgroundServices
{
    public class InvoiceAutomationWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public InvoiceAutomationWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                // Hệ thống tự động thức dậy vào lúc 1:00 sáng mỗi ngày để quét
                if (now.Hour == 1)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var tuitionService = scope.ServiceProvider.GetRequiredService<ITuitionFeeService>();
                        var repo = scope.ServiceProvider.GetRequiredService<ITuitionFeeRepository>();

                        // Worker dùng quyền hệ thống nên lấy toàn bộ lớp ra xử lý
                        var allClasses = await repo.GetAllClassesWithStudentsAsync();

                        foreach (var classObj in allClasses)
                        {
                            var teacherId = classObj.TeacherId; // Lấy đúng ID giáo viên của lớp đó

                            // =========================================================
                            // KỊCH BẢN 1: LỚP THU TRƯỚC (PREPAID) - Chạy vào mùng 1 đầu tháng
                            // =========================================================
                            if (classObj.BillingMethod == "Prepaid" && now.Day == 1)
                            {
                                // Lấy thời gian của tháng liền trước
                                var lastMonth = now.AddMonths(-1);

                                // BƯỚC 1: Tự động chốt sổ cấn trừ của tháng trước
                                try
                                {
                                    await tuitionService.ReconcilePrepaidClassAsync(classObj.ClassId, lastMonth.Month, lastMonth.Year, teacherId);
                                }
                                catch { /* Bỏ qua nếu có lỗi nhỏ để không chết toàn bộ tiến trình */ }

                                // BƯỚC 2: Tự động phát hành hóa đơn của tháng này (sẽ dùng tiền cấn trừ vừa tính)
                                try
                                {
                                    await tuitionService.GenerateInvoicesForClassAsync(classObj.ClassId, new GenerateInvoiceDto
                                    {
                                        PeriodMonth = now.Month,
                                        PeriodYear = now.Year,
                                        DueDate = now.AddDays(5) // Hạn nộp mặc định 5 ngày
                                    }, teacherId);
                                }
                                catch { /* Đã phát hành rồi thì bỏ qua */ }
                            }

                            // =========================================================
                            // KỊCH BẢN 2: LỚP THU SAU (POSTPAID) - Chạy vào ngày cuối cùng của tháng
                            // =========================================================
                            else if (classObj.BillingMethod == "Postpaid" && now.Day == DateTime.DaysInMonth(now.Year, now.Month))
                            {
                                try
                                {
                                    await tuitionService.GenerateInvoicesForClassAsync(classObj.ClassId, new GenerateInvoiceDto
                                    {
                                        PeriodMonth = now.Month,
                                        PeriodYear = now.Year,
                                        DueDate = now.AddDays(5)
                                    }, teacherId);
                                }
                                catch { /* ... */ }
                            }
                        }
                    }
                }

                // Ngủ 1 tiếng rồi kiểm tra lại đồng hồ
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
