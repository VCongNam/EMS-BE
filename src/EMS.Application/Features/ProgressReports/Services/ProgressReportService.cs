using DocumentFormat.OpenXml.Wordprocessing;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Notifications.Services;
using EMS.Application.Features.ProgressReports.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
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
        private readonly INotificationService _notificationService;
        private readonly ILogger<ProgressReportService> _logger;

        public ProgressReportService(IProgressReportRepository reportRepository, ICurrentUserService currentUserService,
            INotificationService notificationService,
    ILogger<ProgressReportService> logger)
        {
            this.reportRepository = reportRepository;
            this.currentUserService = currentUserService;
            _notificationService = notificationService;
            _logger = logger;
        }

        private decimal CalculateGpa(List<Submission> studentSubs)
        {
            if (!studentSubs.Any()) return 0m;

            decimal totalWeighted = studentSubs.Sum(s => (s.Grade ?? 0m) * (s.Assignment.GradeCategory.Weight / 100m));
            decimal totalWeight = studentSubs.Sum(s => s.Assignment.GradeCategory.Weight / 100m);

            return totalWeight > 0 ? Math.Round(totalWeighted / totalWeight, 2) : 0m;
        }

        private decimal CalculateAttendance(List<Attendance> studentAtts, int totalClassSessions)
        {
            if (totalClassSessions <= 0) return 0m;

            int presentCount = studentAtts.Count(a => a.Status == "Present");

            return Math.Round((decimal)presentCount / totalClassSessions * 100, 2);
        }



        private void ValidateClassStatusAndPeriod(Class? cls, Guid classId, int month, int year)
        {
            if (cls == null || cls.IsDeleted == true)
                throw new NotFoundException("Lớp học", classId);

            if (cls.Status == "Archived")
                throw new BadRequestException($"Lớp học đang ở trạng thái '{cls.Status}'. Không thể thao tác báo cáo.");

            var periodStart = new DateOnly(year, month, 1);
            var periodEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

            if (cls.StartDate > periodEnd)
                throw new BadRequestException($"Lớp chưa bắt đầu trong kỳ {month}/{year}. (Ngày bắt đầu: {cls.StartDate:dd/MM/yyyy})");

            if (cls.EndDate < periodStart)
                throw new BadRequestException($"Lớp đã kết thúc trước kỳ {month}/{year}. (Ngày kết thúc: {cls.EndDate:dd/MM/yyyy})");
        }
        public async Task<IEnumerable<ProgressReportResponseDto>> GetClassReportDetailsAsync(Guid classId, int month, int year)
        {
            var currentClass = await reportRepository.GetClassByIdAsync(classId);
            ValidateClassStatusAndPeriod(currentClass, classId, month, year);

            var enrollments = await reportRepository.GetActiveStudentsInClassAsync(classId);
            var existingReports = await reportRepository.GetReportsByClassAndPeriodAsync(classId, month, year);

            var startDateDt = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDateDt = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);

            var allSubs = await reportRepository.GetSubmissionsForCalcAsync(classId, startDateDt, endDateDt);
            var allAtts = await reportRepository.GetAttendancesForCalcAsync(classId, DateOnly.FromDateTime(startDateDt), DateOnly.FromDateTime(endDateDt));
            int totalSessions = await reportRepository.GetTotalSessionsInPeriodAsync(classId, DateOnly.FromDateTime(startDateDt), DateOnly.FromDateTime(endDateDt));

            var result = new List<ProgressReportResponseDto>();
            foreach (var e in enrollments)
            {
                var report = existingReports.FirstOrDefault(r => r.StudentId == e.StudentId);
                var studentSubs = allSubs.Where(s => s.StudentId == e.StudentId).ToList();
                var studentAtts = allAtts.Where(a => a.StudentId == e.StudentId).ToList();

                decimal liveGpa = CalculateGpa(studentSubs);
                decimal liveAtt = CalculateAttendance(studentAtts, totalSessions);

                result.Add(new ProgressReportResponseDto
                {
                    ReportId = report?.ReportId,
                    StudentId = e.StudentId,
                    StudentName = e.Student?.FullName ?? "Unknown",
                    ClassId = e.ClassId,
                    PeriodMonth = month,
                    PeriodYear = year,
                    Title = report?.Title ?? $"Báo cáo học tập tháng {month}/{year}",
                    Content = report?.Content,
                    Status = report?.Status ?? "Draft",
                    Gpa = (report?.Status == "Published") ? (report.Gpa ?? liveGpa) : liveGpa,
                    AttendanceRate = (report?.Status == "Published") ? (report.AttendanceRate ?? liveAtt) : liveAtt,
                    GradeHistory = studentSubs.Select(s => new GradeHistoryDto
                    {
                        AssignmentTitle = s.Assignment.Title,
                        CategoryName = s.Assignment.GradeCategory.Name,
                        Weight = s.Assignment.GradeCategory.Weight,
                        Grade = s.Grade,
                        Date = s.Assignment.DueDate
                    }).OrderBy(x => x.Date).ToList(),
                    AttendanceHistory = studentAtts.Select(a => new AttendanceHistoryDto
                    {
                        Date = a.Session.Date,
                        Status = a.Status,
                        Note = a.Note,
                        Topic = a.Session.Topic
                    }).OrderBy(x => x.Date).ToList(),
                    CreatedAt = report?.CreatedAt,
                    UpdatedAt = report?.UpdatedAt
                });
            }
            return result;
        }

        public async Task<Guid> CreateReportAsync(CreateProgressReportDto request)
        {
            var now = DateTime.UtcNow;
            if (request.PeriodYear > now.Year || (request.PeriodYear == now.Year && request.PeriodMonth > now.Month))
                throw new BadRequestException("Không thể tạo báo cáo cho tháng trong tương lai.");

            var currentClass = await reportRepository.GetClassByIdAsync(request.ClassId);
            ValidateClassStatusAndPeriod(currentClass, request.ClassId, request.PeriodMonth, request.PeriodYear);

            if (request.Status == "Ready" && string.IsNullOrWhiteSpace(request.Content))
                throw new BadRequestException("Vui lòng nhập nội dung nhận xét trước khi đặt trạng thái Sẵn sàng.");

            var exist = await reportRepository.IsReportExistAsync(request.StudentId, request.ClassId, request.PeriodMonth, request.PeriodYear);
            if (exist) throw new BadRequestException($"Báo cáo kỳ {request.PeriodMonth}/{request.PeriodYear} đã tồn tại.");

            var startDate = new DateOnly(request.PeriodYear, request.PeriodMonth, 1);
            var endDate = new DateOnly(request.PeriodYear, request.PeriodMonth, DateTime.DaysInMonth(request.PeriodYear, request.PeriodMonth));

            var subs = await reportRepository.GetSubmissionsForCalcAsync(request.ClassId, startDate.ToDateTime(TimeOnly.MinValue), endDate.ToDateTime(TimeOnly.MaxValue));
            var atts = await reportRepository.GetAttendancesForCalcAsync(request.ClassId, startDate, endDate);
            int totalSessions = await reportRepository.GetTotalSessionsInPeriodAsync(request.ClassId, startDate, endDate);

            var report = new ProgressReport
            {
                ReportId = Guid.NewGuid(),
                StudentId = request.StudentId,
                ClassId = request.ClassId,
                TeacherId = currentUserService.UserId,
                PeriodMonth = (short)request.PeriodMonth,
                PeriodYear = request.PeriodYear,
                Title = request.Title ?? $"Báo cáo học tập tháng {request.PeriodMonth}/{request.PeriodYear}",
                Content = request.Content,
                Status = request.Status,
                Gpa = CalculateGpa(subs.Where(s => s.StudentId == request.StudentId).ToList()),
                AttendanceRate = CalculateAttendance(atts.Where(a => a.StudentId == request.StudentId).ToList(), totalSessions),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await reportRepository.AddAsync(report);
            return report.ReportId;
        }


        public async Task UpdateReportAsync(Guid id, UpdateProgressReportDto request)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new NotFoundException("Báo cáo", id);
            if (report.TeacherId != currentUserService.UserId) throw new ForbiddenAccessException();
            if (report.Status == "Published") throw new BadRequestException("Báo cáo đã gửi không thể chỉnh sửa.");

            ValidateClassStatusAndPeriod(report.Class, report.ClassId, (int)report.PeriodMonth!, (int)report.PeriodYear!);

            if (request.Status == "Ready" && string.IsNullOrWhiteSpace(request.Content))
                throw new BadRequestException("Nội dung nhận xét không được để trống khi đặt trạng thái Sẵn sàng.");

            var startDate = new DateOnly(report.PeriodYear!.Value, (int)report.PeriodMonth!.Value, 1);
            var endDate = new DateOnly(report.PeriodYear!.Value, (int)report.PeriodMonth!.Value, DateTime.DaysInMonth(report.PeriodYear!.Value, (int)report.PeriodMonth!.Value));

            var subs = await reportRepository.GetSubmissionsForCalcAsync(report.ClassId, startDate.ToDateTime(TimeOnly.MinValue), endDate.ToDateTime(TimeOnly.MaxValue));
            var atts = await reportRepository.GetAttendancesForCalcAsync(report.ClassId, startDate, endDate);
            int totalSessions = await reportRepository.GetTotalSessionsInPeriodAsync(report.ClassId, startDate, endDate);

            report.Title = request.Title;
            report.Content = request.Content;
            report.Status = request.Status;
            report.Gpa = CalculateGpa(subs.Where(s => s.StudentId == report.StudentId).ToList());
            report.AttendanceRate = CalculateAttendance(atts.Where(a => a.StudentId == report.StudentId).ToList(), totalSessions);
            report.UpdatedAt = DateTime.UtcNow;

            await reportRepository.UpdateAsync(report);
        }

        public async Task SendReportAsync(Guid id)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new NotFoundException("Báo cáo", id);
            if (report.TeacherId != currentUserService.UserId) throw new ForbiddenAccessException("Bạn không có quyền chỉnh sửa báo cáo này.");
            if (report.Status == "Published") return;

            report.Status = "Published";
            report.UpdatedAt = DateTime.UtcNow;
            await reportRepository.UpdateAsync(report);

            try
            {
                if (report.Student != null)
                {
                    await _notificationService.SendNotificationAsync(
                        targetAccountId: report.Student.AccountId,
                        studentId: report.StudentId,
                        title: "Báo cáo học tập mới",
                        content: $"Báo cáo tháng {report.PeriodMonth}/{report.PeriodYear} lớp {report.Class.ClassName} đã có sẵn.",
                        actionUrl: $"/student/reports/{report.ReportId}",
                        type: "Report"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi Notification: {ex.Message}");
            }
        }
        public async Task DeleteReportAsync(Guid id)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new NotFoundException("Báo cáo", id);
            if (report.TeacherId != currentUserService.UserId) throw new ForbiddenAccessException("Bạn không có quyền chỉnh sửa báo cáo này.");
            if (report.Status == "Published")
                throw new BadRequestException("Không thể xóa báo cáo đã gửi cho phụ huynh.");

            await reportRepository.DeleteAsync(report);
        }

        public async Task<ProgressReportResponseDto> GetReportDetailAsync(Guid id)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new NotFoundException("Báo cáo không tồn tại.");

            var startDate = new DateOnly(report.PeriodYear!.Value, (int)report.PeriodMonth!.Value, 1);
            var endDate = new DateOnly(report.PeriodYear!.Value, (int)report.PeriodMonth!.Value, DateTime.DaysInMonth(report.PeriodYear!.Value, (int)report.PeriodMonth!.Value));

            var subs = await reportRepository.GetSubmissionsForCalcAsync(report.ClassId, startDate.ToDateTime(TimeOnly.MinValue), endDate.ToDateTime(TimeOnly.MaxValue));
            var atts = await reportRepository.GetAttendancesForCalcAsync(report.ClassId, startDate, endDate);
            int totalSessions = await reportRepository.GetTotalSessionsInPeriodAsync(report.ClassId, startDate, endDate);

            var studentSubs = subs.Where(s => s.StudentId == report.StudentId).ToList();
            var studentAtts = atts.Where(a => a.StudentId == report.StudentId).ToList();

            return new ProgressReportResponseDto
            {
                ReportId = report.ReportId,
                StudentId = report.StudentId,
                StudentName = report.Student?.FullName ?? "N/A",
                ClassId = report.ClassId,
                PeriodMonth = (int)report.PeriodMonth!,
                PeriodYear = (int)report.PeriodYear!,
                Title = report.Title,
                Content = report.Content,
                Status = report.Status ?? "Draft",
                Gpa = (report.Status == "Published") ? (report.Gpa ?? 0m) : CalculateGpa(studentSubs),
                AttendanceRate = (report.Status == "Published") ? (report.AttendanceRate ?? 0m) : CalculateAttendance(studentAtts, totalSessions),

                GradeHistory = studentSubs.Select(s => new GradeHistoryDto
                {
                    AssignmentTitle = s.Assignment.Title,
                    CategoryName = s.Assignment.GradeCategory.Name,
                    Weight = s.Assignment.GradeCategory.Weight,
                    Grade = s.Grade,
                    Date = s.Assignment.DueDate
                }).OrderBy(x => x.Date).ToList(),

                AttendanceHistory = studentAtts.Select(a => new AttendanceHistoryDto
                {
                    Date = a.Session.Date,
                    Status = a.Status,
                    Note = a.Note,
                    Topic = a.Session.Topic
                }).OrderBy(x => x.Date).ToList(),

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

            var reportDeadline = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddDays(4);

            foreach (var c in classes)
            {
                var classReports = reports.Where(r => r.ClassId == c.ClassId).ToList();

                studentCounts.TryGetValue(c.ClassId, out int totalStudents);

                int readyCount = classReports.Count(r => r.Status == "Ready");
                int publishedCount = classReports.Count(r => r.Status == "Published");
                int createdReports = readyCount + publishedCount;

                double completionRate = totalStudents > 0 ? Math.Round((double)createdReports / totalStudents * 100, 1) : 0;

                var timeRemaining = reportDeadline - DateTime.UtcNow;
                bool isNearDeadline = timeRemaining.TotalHours <= 48 && timeRemaining.TotalHours >= 0 && createdReports < totalStudents;

                dashboardData.ClassSummaries.Add(new ClassReportSummaryItemDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    Room = c.Room,
                    TotalStudents = totalStudents,
                    ReadyCount = readyCount,
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
