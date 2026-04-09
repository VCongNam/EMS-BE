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

        public ProgressReportService(IProgressReportRepository reportRepository, ICurrentUserService currentUserService)
        {
            this.reportRepository = reportRepository;
            this.currentUserService = currentUserService;
        }

        // --- CÁC HÀM PHỤ TRỢ TÍNH TOÁN (Sửa kiểu trả về thành decimal để khớp với Entity) ---
        private decimal CalculateGpa(List<Submission> studentSubs)
        {
            if (!studentSubs.Any()) return 0m;

            // Chuyển hết về decimal để tính toán đồng bộ
            decimal totalWeighted = studentSubs.Sum(s => (s.Grade ?? 0m) * (s.Assignment.GradeCategory.Weight / 100m));
            decimal totalWeight = studentSubs.Sum(s => s.Assignment.GradeCategory.Weight / 100m);

            return totalWeight > 0 ? Math.Round(totalWeighted / totalWeight, 2) : 0m;
        }

        private decimal CalculateAttendance(List<Attendance> studentAtts)
        {
            if (!studentAtts.Any()) return 0m;
            int presentCount = studentAtts.Count(a => a.Status == "Present");
            return Math.Round((decimal)presentCount / studentAtts.Count * 100, 2);
        }

        // --- CÁC CHỨC NĂNG CHÍNH ---

        public async Task<IEnumerable<ProgressReportResponseDto>> GetClassReportDetailsAsync(Guid classId, int month, int year)
        {
            var enrollments = await reportRepository.GetActiveStudentsInClassAsync(classId);
            var existingReports = await reportRepository.GetReportsByClassAndPeriodAsync(classId, month, year);

            // Chỉ định rõ DateTimeKind để tránh lỗi Supabase/Postgres
            var startDateDt = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDateDt = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);

            var allSubs = await reportRepository.GetSubmissionsForCalcAsync(classId, startDateDt, endDateDt);
            var allAtts = await reportRepository.GetAttendancesForCalcAsync(classId, DateOnly.FromDateTime(startDateDt), DateOnly.FromDateTime(endDateDt));

            var result = new List<ProgressReportResponseDto>();

            foreach (var e in enrollments)
            {
                var report = existingReports.FirstOrDefault(r => r.StudentId == e.StudentId);

                // Tính toán "Live"
                decimal liveGpa = CalculateGpa(allSubs.Where(s => s.StudentId == e.StudentId).ToList());
                decimal liveAtt = CalculateAttendance(allAtts.Where(a => a.StudentId == e.StudentId).ToList());

                result.Add(new ProgressReportResponseDto
                {
                    ReportId = report?.ReportId,
                    StudentId = e.StudentId,
                    StudentName = e.Student?.FullName ?? "Unknown",
                    ClassId = e.ClassId,
                    PeriodMonth = month,
                    PeriodYear = year,
                    Title = report?.Title,
                    Content = report?.Content,
                    Status = report?.Status ?? "Ready",
                    // Fix lỗi ?? giữa decimal? và decimal
                    Gpa = report?.Gpa ?? liveGpa,
                    AttendanceRate = report?.AttendanceRate ?? liveAtt,
                    CreatedAt = report?.CreatedAt,
                    UpdatedAt = report?.UpdatedAt
                });
            }
            return result;
        }

        public async Task<Guid> CreateReportAsync(CreateProgressReportDto request)
        {
            var exist = await reportRepository.IsReportExistAsync(request.StudentId, request.ClassId, request.PeriodMonth, request.PeriodYear);
            if (exist) throw new Exception("Báo cáo tháng này của học sinh đã tồn tại.");

            var startDateDt = new DateTime(request.PeriodYear, request.PeriodMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDateDt = new DateTime(request.PeriodYear, request.PeriodMonth, DateTime.DaysInMonth(request.PeriodYear, request.PeriodMonth), 23, 59, 59, DateTimeKind.Utc);

            var subs = await reportRepository.GetSubmissionsForCalcAsync(request.ClassId, startDateDt, endDateDt);
            var atts = await reportRepository.GetAttendancesForCalcAsync(request.ClassId, DateOnly.FromDateTime(startDateDt), DateOnly.FromDateTime(endDateDt));

            decimal gpa = CalculateGpa(subs.Where(s => s.StudentId == request.StudentId).ToList());
            decimal attRate = CalculateAttendance(atts.Where(a => a.StudentId == request.StudentId).ToList());

            var report = new ProgressReport
            {
                ReportId = Guid.NewGuid(),
                StudentId = request.StudentId,
                ClassId = request.ClassId,
                TeacherId = currentUserService.UserId,
                PeriodMonth = (short)request.PeriodMonth, // Ép kiểu về short nếu Entity yêu cầu
                PeriodYear = request.PeriodYear,
                Title = request.Title,
                Content = request.Content,
                Status = request.Status,
                Gpa = gpa,
                AttendanceRate = attRate,
                CreatedAt = DateTime.UtcNow
            };

            await reportRepository.AddAsync(report);
            return report.ReportId;
        }

        public async Task UpdateReportAsync(Guid id, UpdateProgressReportDto request)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new Exception("Không tìm thấy báo cáo.");
            if (report.TeacherId != currentUserService.UserId) throw new Exception("Bạn không có quyền sửa.");
            if (report.Status == "Published") throw new Exception("Báo cáo đã gửi không thể chỉnh sửa.");

            // Áp dụng cách dùng !.Value và ép kiểu (int)
            var startDateDt = new DateTime(report.PeriodYear!.Value, (int)report.PeriodMonth!.Value, 1, 0, 0, 0, DateTimeKind.Utc);

            var endDateDt = new DateTime(
                report.PeriodYear!.Value,
                (int)report.PeriodMonth!.Value,
                DateTime.DaysInMonth(report.PeriodYear!.Value, (int)report.PeriodMonth!.Value),
                23, 59, 59,
                DateTimeKind.Utc);

            var subs = await reportRepository.GetSubmissionsForCalcAsync(report.ClassId, startDateDt, endDateDt);
            var atts = await reportRepository.GetAttendancesForCalcAsync(report.ClassId, DateOnly.FromDateTime(startDateDt), DateOnly.FromDateTime(endDateDt));

            report.Title = request.Title;
            report.Content = request.Content;
            report.Status = request.Status;
            report.Gpa = CalculateGpa(subs.Where(s => s.StudentId == report.StudentId).ToList());
            report.AttendanceRate = CalculateAttendance(atts.Where(a => a.StudentId == report.StudentId).ToList());
            report.UpdatedAt = DateTime.UtcNow;

            await reportRepository.UpdateAsync(report);
        }

        public async Task SendReportAsync(Guid id)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new Exception("Không tìm thấy báo cáo.");
            if (report.TeacherId != currentUserService.UserId) throw new Exception("Bạn không có quyền.");
            if (report.Status == "Published") return;

            report.Status = "Published";
            report.UpdatedAt = DateTime.UtcNow;
            await reportRepository.UpdateAsync(report);
        }

        public async Task DeleteReportAsync(Guid id)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new Exception("Không tìm thấy.");
            if (report.TeacherId != currentUserService.UserId) throw new Exception("Bạn không có quyền xóa.");
            if (report.Status == "Published") throw new Exception("Không thể xóa báo cáo đã gửi.");

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
                StudentName = report.Student?.FullName ?? "Unknown",
                ClassId = report.ClassId,
                TeacherId = report.TeacherId,
                PeriodMonth = report.PeriodMonth,
                PeriodYear = report.PeriodYear,
                Title = report.Title,
                Content = report.Content,
                Status = report.Status ?? "Draft",
                Gpa = report.Gpa ?? 0m,
                AttendanceRate = report.AttendanceRate ?? 0m,
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt
            };
        }

        public async Task<ProgressReportDashboardDto> GetClassesSummaryAsync(int month, int year, string? searchTerm = null)
        {
            var teacherId = currentUserService.UserId;
            var classes = await reportRepository.GetClassesByTeacherAndPeriodAsync(teacherId, month, year, searchTerm);

            var dashboardData = new ProgressReportDashboardDto
            {
                TotalClasses = classes.Count,
                ClassSummaries = new List<ClassReportSummaryItemDto>()
            };

            if (!classes.Any()) return dashboardData;

            var classIds = classes.Select(c => c.ClassId).ToList();
            var studentCounts = await reportRepository.GetActiveStudentCountsByClassesAsync(classIds);
            var reports = await reportRepository.GetReportsByClassesAndPeriodAsync(classIds, month, year);

            int totalSystemStudents = 0;
            int totalSystemCreatedReports = 0;

            // Quy tắc Hạn chót: Ngày 5 của tháng kế tiếp
            var reportDeadline = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddDays(4);

            foreach (var c in classes)
            {
                var classReports = reports.Where(r => r.ClassId == c.ClassId).ToList();

                // Dùng TryGetValue để tránh lỗi KeyNotFound
                studentCounts.TryGetValue(c.ClassId, out int totalStudents);

                int draftCount = classReports.Count(r => r.Status == "Draft");
                int publishedCount = classReports.Count(r => r.Status == "Published");
                int createdReports = draftCount + publishedCount;

                double completionRate = totalStudents > 0 ? Math.Round((double)createdReports / totalStudents * 100, 1) : 0;

                var timeRemaining = reportDeadline - DateTime.UtcNow;
                bool isNearDeadline = timeRemaining.TotalHours <= 48 && timeRemaining.TotalHours >= 0 && createdReports < totalStudents;

                dashboardData.ClassSummaries.Add(new ClassReportSummaryItemDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    Room = c.Room,
                    TotalStudents = totalStudents,
                    DraftCount = draftCount,
                    PublishedCount = publishedCount,
                    CompletionRate = completionRate,
                    Deadline = reportDeadline,
                    IsNearDeadline = isNearDeadline,
                    LastUpdated = classReports.Any() ? classReports.Max(r => r.UpdatedAt) : null
                });

                totalSystemStudents += totalStudents;
                totalSystemCreatedReports += createdReports;
            }

            dashboardData.OverallCompletionRate = totalSystemStudents > 0
                ? Math.Round((double)totalSystemCreatedReports / totalSystemStudents * 100, 1)
                : 0;

            return dashboardData;
        }
    }
}
