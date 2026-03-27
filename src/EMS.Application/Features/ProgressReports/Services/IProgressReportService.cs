using EMS.Application.Features.ProgressReports.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.ProgressReports.Services
{
    public interface IProgressReportService
    {
        Task<Guid> CreateReportAsync(CreateProgressReportDto request);
        Task<ProgressReportResponseDto> GetReportByIdAsync(Guid reportId);
        Task<IEnumerable<ProgressReportResponseDto>> GetMyTeachingReportsAsync();
        Task UpdateReportAsync(Guid reportId, UpdateProgressReportDto request);
        Task DeleteReportAsync(Guid reportId);
        Task SendReportAsync(Guid reportId);
    }
}
