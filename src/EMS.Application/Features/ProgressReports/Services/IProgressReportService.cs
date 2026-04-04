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
        Task UpdateReportAsync(Guid id, UpdateProgressReportDto request);
        Task DeleteReportAsync(Guid id);
        Task<ProgressReportResponseDto> GetReportDetailAsync(Guid id);
        Task<IEnumerable<ProgressReportResponseDto>> GetClassReportDetailsAsync(Guid classId, int month, int year);
        Task SendReportAsync(Guid id); 
    }
}
