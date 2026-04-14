using EMS.Application.Features.Feedbacks.Dtos;
using EMS.Application.Features.Notifications.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Feedbacks.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository feedbackRepository;
        private readonly INotificationService notificationService;
        private readonly ILogger<FeedbackService> logger;

        public FeedbackService(
            IFeedbackRepository feedbackRepository,
            INotificationService notificationService,
            ILogger<FeedbackService> logger)
        {
            this.feedbackRepository = feedbackRepository;
            this.notificationService = notificationService;
            this.logger = logger;
        }

        public async Task CreateFeedbackAsync(Guid uid, CreateFeedbackDto dto)
        {
            var fb = new SystemFeedback
            {
                FeedbackId = Guid.NewGuid(),
                SenderId = uid,
                Title = dto.Title,
                Content = dto.Content,
                Type = dto.Type,
                CreatedAt = DateTime.UtcNow
            };
            await feedbackRepository.AddAsync(fb);
        }

        public async Task<IEnumerable<FeedbackSummaryDto>> GetAdminListAsync(string? t, string? s)
        {
            var data = await feedbackRepository.GetAllAsync(t, s);
            return data.Select(f => new FeedbackSummaryDto
            {
                FeedbackId = f.FeedbackId,
                SenderName = f.Sender?.FullName ?? "Unknown",
                Title = f.Title,
                Type = f.Type,
                Status = f.Status,
                CreatedAt = (DateTime)f.CreatedAt
            }).ToList();
        }

        public async Task ProcessFeedbackAsync(Guid fid, ProcessFeedbackDto dto)
        {
            var fb = await feedbackRepository.GetByIdAsync(fid);
            if (fb != null)
            {
                // 1. Cập nhật dữ liệu qua Repository
                fb.Status = dto.NewStatus;
                fb.AdminReply = dto.AdminReply;
                fb.UpdatedAt = DateTime.UtcNow;
                await feedbackRepository.UpdateAsync(fb);

                // 2. Gửi thông báo (Sử dụng đúng mẫu try-catch của bạn)
                try
                {
                    await notificationService.SendNotificationAsync(
                        targetAccountId: fb.SenderId,
                        studentId: null,
                        title: "Phản hồi hệ thống",
                        content: $"Quản trị viên đã xử lý góp ý: '{fb.Title}'. Trạng thái: {fb.Status}",
                        actionUrl: "/teacher/feedback/history",
                        type: "Feedback"
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError($"Lỗi gửi thông báo Feedback: {ex.Message}");
                }
            }
        }
    }
}
