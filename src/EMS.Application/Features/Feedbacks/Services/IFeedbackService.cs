using EMS.Application.Features.Feedbacks.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Feedbacks.Services
{
    public interface IFeedbackService
    {
        Task CreateFeedbackAsync(Guid userId, CreateFeedbackDto dto);
        Task<IEnumerable<FeedbackSummaryDto>> GetAdminListAsync(string? type, string? status);
        Task ProcessFeedbackAsync(Guid feedbackId, ProcessFeedbackDto dto);
        Task<IEnumerable<TeacherFeedbackHistoryDto>> GetTeacherHistoryAsync(Guid userId);
    }
}
