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
        private readonly ICurrentUserService currentUserService;

        public ProgressReportService(IProgressReportRepository reportRepository,ICurrentUserService currentUserService)
        {
            this.reportRepository = reportRepository;
            this.currentUserService = currentUserService;
        }

        public async Task<Guid> CreateReportAsync(CreateProgressReportDto request)
        {
            var report = new ProgressReport
            {
                ReportId = Guid.NewGuid(),
                StudentId = request.StudentId,
                ClassId = request.ClassId,
                TeacherId = currentUserService.UserId,
                Title = request.Title,
                Content = request.Content,
                Status = request.Status, // Thường sẽ là "Draft"
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await reportRepository.AddAsync(report);
            return report.ReportId;
        }

        public async Task UpdateReportAsync(Guid id, UpdateProgressReportDto request)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new Exception("Report not found.");

            if (report.TeacherId != currentUserService.UserId)
                throw new Exception("You do not have permission to edit this report.");

            report.Title = request.Title;
            report.Content = request.Content;
            report.UpdatedAt = DateTime.UtcNow;

            await reportRepository.UpdateAsync(report);
        }

        public async Task DeleteReportAsync(Guid id)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new Exception("Report not found.");

            if (report.TeacherId != currentUserService.UserId)
                throw new Exception("You do not have permission to delete this report.");

            await reportRepository.DeleteAsync(report);
        }

        public async Task<ProgressReportResponseDto> GetReportDetailAsync(Guid id)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new Exception("Report not found.");

            return new ProgressReportResponseDto
            {
                ReportId = report.ReportId,
                StudentId = report.StudentId,
                StudentName = report.Student?.StudentNavigation?.FullName ?? null!,
                ClassId = report.ClassId,
                ClassName = report.Class?.ClassName ?? null!,
                TeacherId = report.TeacherId,
                TeacherName = report.Teacher?.TeacherNavigation?.FullName ?? null!,
                Title = report.Title ?? null!,
                Content = report.Content,
                Status = report.Status ?? null!,
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt
            };
        }

        public async Task<IEnumerable<ProgressReportResponseDto>> GetReportsForStudentAsync(Guid studentId, Guid classId)
        {
            var reports = await reportRepository.GetReportsByStudentAndClassAsync(studentId, classId);

            return reports
                .Where(r => r.Status == "Published") // Chốt chặn bảo mật
                .Select(r => new ProgressReportResponseDto
                {
                    ReportId = r.ReportId,
                    StudentId = r.StudentId,
                    ClassId = r.ClassId,
                    ClassName = r.Class?.ClassName ?? null!,
                    TeacherId = r.TeacherId,
                    TeacherName = r.Teacher?.TeacherNavigation?.FullName ?? null!,
                    Title = r.Title ?? null!,
                    Content = r.Content,
                    Status = r.Status ?? null!,
                    CreatedAt = r.CreatedAt
                });
        }

        public async Task<IEnumerable<ProgressReportResponseDto>> GetReportsByClassAsync(Guid classId)
        {
            var reports = await reportRepository.GetReportsByClassIdAsync(classId);

            return reports.Select(r => new ProgressReportResponseDto
            {
                ReportId = r.ReportId,
                StudentId = r.StudentId,
                StudentName = r.Student?.StudentNavigation?.FullName ?? null!,
                ClassId = r.ClassId,
                ClassName = r.Class?.ClassName ?? null!,
                TeacherId = r.TeacherId,
                TeacherName = r.Teacher?.TeacherNavigation?.FullName ?? null!,
                Title = r.Title ?? null!,
                Status = r.Status ?? null!,
                Content = r.Content,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            });
        }

        public async Task<IEnumerable<ProgressReportResponseDto>> GetReportsByTeacherAsync()
        {
            var teacherId = currentUserService.UserId;
            var reports = await reportRepository.GetReportsByTeacherAsync(teacherId);

            return reports.Select(r => new ProgressReportResponseDto
            {
                ReportId = r.ReportId,
                StudentId = r.StudentId,
                StudentName = r.Student?.StudentNavigation?.FullName ?? null!,
                ClassId = r.ClassId,
                ClassName = r.Class?.ClassName ?? null!,
                TeacherId = r.TeacherId,
                TeacherName = r.Teacher?.TeacherNavigation?.FullName ?? null!,
                Title = r.Title ?? null!,
                Status = r.Status ?? null!,
                Content = r.Content,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            });
        }

        public async Task SendReportAsync(Guid id)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new Exception("Report not found.");

            if (report.TeacherId != currentUserService.UserId)
                throw new Exception("You do not have permission to send this report.");

            if (report.Status == "Published")
                throw new Exception("This report has already been sent.");

            report.Status = "Published";
            report.UpdatedAt = DateTime.UtcNow;

            await reportRepository.UpdateAsync(report);

            // Logic gửi Email / Thông báo Notification có thể thêm vào đây
        }
    }
}
