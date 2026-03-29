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
        // CRUD Cơ bản
        Task<Guid> CreateReportAsync(CreateProgressReportDto request);
        Task UpdateReportAsync(Guid id, UpdateProgressReportDto request);
        Task DeleteReportAsync(Guid id);
        Task<ProgressReportResponseDto> GetReportDetailAsync(Guid id);

        // Các Use Case lấy danh sách
        Task<IEnumerable<ProgressReportResponseDto>> GetReportsForStudentAsync(Guid studentId, Guid classId);
        Task<IEnumerable<ProgressReportResponseDto>> GetReportsByClassAsync(Guid classId);
        Task<IEnumerable<ProgressReportResponseDto>> GetReportsByTeacherAsync();

        // Use Case gửi báo cáo
        Task SendReportAsync(Guid id);
    }
}
