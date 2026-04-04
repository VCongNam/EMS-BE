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

        // Đã loại bỏ hoàn toàn ApplicationDbContext để đảm bảo Clean Architecture
        public ProgressReportService(IProgressReportRepository reportRepository, ICurrentUserService currentUserService)
        {
            this.reportRepository = reportRepository;
            this.currentUserService = currentUserService;
        }

        public async Task<Guid> CreateReportAsync(CreateProgressReportDto request)
        {
            // Kiểm tra trùng lặp thông qua Repository
            var exist = await reportRepository.IsReportExistAsync(request.StudentId, request.ClassId, request.PeriodMonth, request.PeriodYear);
            if (exist) throw new Exception("Báo cáo tháng này của học sinh đã tồn tại, vui lòng dùng tính năng Cập nhật.");

            var report = new ProgressReport
            {
                ReportId = Guid.NewGuid(),
                StudentId = request.StudentId,
                ClassId = request.ClassId,
                TeacherId = currentUserService.UserId,
                PeriodMonth = request.PeriodMonth,
                PeriodYear = request.PeriodYear,
                Title = request.Title,
                Content = request.Content,
                Status = request.Status, // Nhận giá trị "Draft" hoặc "Published" từ giao diện
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await reportRepository.AddAsync(report);
            return report.ReportId;
        }

        public async Task UpdateReportAsync(Guid id, UpdateProgressReportDto request)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new Exception("Không tìm thấy báo cáo.");

            // Kiểm tra quyền (chỉ người tạo mới được sửa)
            if (report.TeacherId != currentUserService.UserId)
                throw new Exception("Bạn không có quyền sửa báo cáo này.");

            // Chốt chặn nghiệp vụ: Đã gửi thì không được sửa
            if (report.Status == "Published")
                throw new Exception("Báo cáo đã gửi cho phụ huynh không thể chỉnh sửa.");

            report.Title = request.Title;
            report.Content = request.Content;
            report.Status = request.Status;
            report.UpdatedAt = DateTime.UtcNow;

            await reportRepository.UpdateAsync(report);
        }

        public async Task DeleteReportAsync(Guid id)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new Exception("Không tìm thấy báo cáo.");

            if (report.TeacherId != currentUserService.UserId)
                throw new Exception("Bạn không có quyền xóa báo cáo này.");

            if (report.Status == "Published")
                throw new Exception("Không thể xóa báo cáo đã gửi cho phụ huynh.");

            await reportRepository.DeleteAsync(report);
        }

        public async Task<ProgressReportResponseDto> GetReportDetailAsync(Guid id)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new Exception("Không tìm thấy báo cáo.");

            return new ProgressReportResponseDto
            {
                ReportId = report.ReportId,
                StudentId = report.StudentId,
                StudentName = report.Student?.StudentNavigation?.FullName ?? "Unknown",
                ClassId = report.ClassId,
                ClassName = report.Class?.ClassName ?? "Unknown",
                TeacherId = report.TeacherId,
                PeriodMonth = report.PeriodMonth,
                PeriodYear = report.PeriodYear,
                Title = report.Title,
                Content = report.Content,
                Status = report.Status,
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt
            };
        }

        public async Task<IEnumerable<ProgressReportResponseDto>> GetClassReportDetailsAsync(Guid classId, int month, int year)
        {
            // 1. Lấy danh sách học sinh đang học từ Repository
            var enrollments = await reportRepository.GetActiveStudentsInClassAsync(classId);

            // 2. Lấy các báo cáo đã viết trong tháng đó
            var existingReports = await reportRepository.GetReportsByClassAndPeriodAsync(classId, month, year);

            var result = new List<ProgressReportResponseDto>();

            // 3. Map dữ liệu để trả về cho UI
            foreach (var e in enrollments)
            {
                var report = existingReports.FirstOrDefault(r => r.StudentId == e.StudentId);

                result.Add(new ProgressReportResponseDto
                {
                    ReportId = report?.ReportId, // Sẽ là null nếu học sinh này chưa có báo cáo
                    StudentId = e.StudentId,
                    StudentName = e.Student?.StudentNavigation?.FullName ?? "Unknown",
                    ClassId = e.ClassId,
                    PeriodMonth = month,
                    PeriodYear = year,
                    Title = report?.Title,
                    Content = report?.Content,
                    Status = report?.Status ?? "Ready", // Mặc định là "Ready" (Sẵn sàng) nếu chưa tạo
                    Gpa = 8.5, // TODO: Cập nhật logic lấy điểm thực tế
                    AttendanceRate = 100.0, // TODO: Cập nhật logic lấy chuyên cần thực tế
                    UpdatedAt = report?.UpdatedAt
                });
            }
            return result;
        }
    }
}
