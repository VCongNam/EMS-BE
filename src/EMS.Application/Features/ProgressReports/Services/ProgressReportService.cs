using EMS.Application.Common.Interfaces;
using EMS.Application.Features.ProgressReports.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.ProgressReports.Services
{
    public class ProgressReportService : IProgressReportService
    {
        private readonly IProgressReportRepository reportRepository;
        private readonly ICurrentUserService currentServiceUser;

        public ProgressReportService(IProgressReportRepository reportRepository, ICurrentUserService currentServiceUser)
        {
            this.reportRepository = reportRepository;
            this.currentServiceUser = currentServiceUser;
        }

        public async Task<Guid> CreateReportAsync(CreateProgressReportDto request)
        {
            var newReport = new ProgressReport
            {
                ReportId = Guid.NewGuid(),
                TeacherId = currentServiceUser.UserId, // Lấy TeacherId từ JWT Token
                StudentId = request.StudentId,
                ClassId = request.ClassId,
                Title = request.Title,
                Content = request.Content,
                Status = "Draft", // Mặc định là bản nháp khi mới tạo
                CreatedAt = DateTime.UtcNow
            };

            await reportRepository.AddAsync(newReport);
            return newReport.ReportId;
        }

        public async Task<ProgressReportResponseDto> GetReportByIdAsync(Guid reportId)
        {
            var report = await reportRepository.GetByIdWithDetailsAsync(reportId);
            if (report == null) throw new Exception("Progress report not found!");

            return new ProgressReportResponseDto
            {
                ReportId = report.ReportId,
                StudentId = report.StudentId,
                StudentName = report.Student?.StudentNavigation?.FullName ?? "Unknown",
                ClassId = report.ClassId,
                ClassName = report.Class?.ClassName ?? "Unknown",
                Title = report.Title,
                Content = report.Content,
                Status = report.Status,
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt
            };
        }

        public async Task<IEnumerable<ProgressReportResponseDto>> GetMyTeachingReportsAsync()
        {
            var reports = await reportRepository.GetReportsByTeacherIdAsync(currentServiceUser.UserId);

            return reports.Select(r => new ProgressReportResponseDto
            {
                ReportId = r.ReportId,
                StudentId = r.StudentId,
                StudentName = r.Student?.StudentNavigation?.FullName ?? "Unknown",
                ClassId = r.ClassId,
                ClassName = r.Class?.ClassName ?? "Unknown",
                Title = r.Title,
                Content = r.Content,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            });
        }

        public async Task UpdateReportAsync(Guid reportId, UpdateProgressReportDto request)
        {
            var report = await reportRepository.GetByIdAsync(reportId);

            if (report == null) throw new Exception("Progress report not found!");

            // BẢO MẬT: Chỉ người tạo mới được sửa
            if (report.TeacherId != currentServiceUser.UserId)
                throw new Exception("Access Denied: You are not the author of this report!");

            report.Title = request.Title;
            report.Content = request.Content;
            report.UpdatedAt = DateTime.UtcNow;

            await reportRepository.UpdateAsync(report);
        }

        public async Task DeleteReportAsync(Guid reportId)
        {
            var report = await reportRepository.GetByIdAsync(reportId);

            if (report == null) throw new Exception("Progress report not found!");

            // BẢO MẬT: Chỉ người tạo mới được xóa
            if (report.TeacherId != currentServiceUser.UserId)
                throw new Exception("Access Denied: You are not authorized to delete this report!");

            await reportRepository.DeleteAsync(report);
        }

        public async Task SendReportAsync(Guid reportId)
        {
            var report = await reportRepository.GetByIdAsync(reportId);

            if (report == null) throw new Exception("Progress report not found!");

            if (report.TeacherId != currentServiceUser.UserId)
                throw new Exception("Access Denied: You are not the author of this report!");

            // Cập nhật trạng thái thành Sent
            report.Status = "Sent";
            report.UpdatedAt = DateTime.UtcNow;

            await reportRepository.UpdateAsync(report);

            // TODO: Gắn logic gửi Email cho phụ huynh (nếu có) vào đây.
        }
    }
}
