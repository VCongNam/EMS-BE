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

        private void ValidateClassStatus(Class? cls, Guid classId)
        {
            if (cls == null || cls.IsDeleted == true)
                throw new NotFoundException("Lớp học", classId);

            if (cls.Status == "Archived")
                throw new BadRequestException($"Lớp học hiện đang ở trạng thái '{cls.Status}'. Chỉ có thể thao tác báo cáo với lớp đang 'Ongoing'.");
        }


        //public async Task<IEnumerable<ProgressReportResponseDto>> GetClassReportDetailsAsync(Guid classId, int month, int year)
        //{
        //    var enrollments = await reportRepository.GetActiveStudentsInClassAsync(classId);
        //    var existingReports = await reportRepository.GetReportsByClassAndPeriodAsync(classId, month, year);

        //    var startDateDt = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        //    var endDateDt = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);

        //    var allSubs = await reportRepository.GetSubmissionsForCalcAsync(classId, startDateDt, endDateDt);
        //    var allAtts = await reportRepository.GetAttendancesForCalcAsync(classId, DateOnly.FromDateTime(startDateDt), DateOnly.FromDateTime(endDateDt));

        //    var result = new List<ProgressReportResponseDto>();

        //    foreach (var e in enrollments)
        //    {
        //        var report = existingReports.FirstOrDefault(r => r.StudentId == e.StudentId);

        //        decimal liveGpa = CalculateGpa(allSubs.Where(s => s.StudentId == e.StudentId).ToList());
        //        decimal liveAtt = CalculateAttendance(allAtts.Where(a => a.StudentId == e.StudentId).ToList());

        //        result.Add(new ProgressReportResponseDto
        //        {
        //            ReportId = report?.ReportId,
        //            StudentId = e.StudentId,
        //            StudentName = e.Student?.FullName ?? "Unknown",
        //            ClassId = e.ClassId,
        //            PeriodMonth = month,
        //            PeriodYear = year,
        //            Title = report?.Title,
        //            Content = report?.Content,
        //            Status = report?.Status ?? "Draft",
        //            Gpa = report?.Gpa ?? liveGpa,
        //            AttendanceRate = report?.AttendanceRate ?? liveAtt,
        //            CreatedAt = report?.CreatedAt,
        //            UpdatedAt = report?.UpdatedAt
        //        });
        //    }
        //    return result;
        //}


        // Trong file ProgressReportService.cs

        public async Task<IEnumerable<ProgressReportResponseDto>> GetClassReportDetailsAsync(Guid classId, int month, int year)
        {
            // 1. Kiểm tra lớp học
            var currentClass = await reportRepository.GetClassByIdAsync(classId);
            ValidateClassStatus(currentClass, classId);

            // 2. Lấy dữ liệu
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

                    // Luôn cập nhật điểm mới nhất nếu chưa gửi chính thức
                    Gpa = (report?.Status == "Published") ? (report.Gpa ?? liveGpa) : liveGpa,
                    AttendanceRate = (report?.Status == "Published") ? (report.AttendanceRate ?? liveAtt) : liveAtt,

                    // --- CHI TIẾT LỊCH SỬ CHO HỌC SINH TỰ SOI CHIẾU ---
                    GradeHistory = studentSubs.Select(s => new GradeHistoryDto
                    {
                        AssignmentTitle = s.Assignment.Title,
                        CategoryName = s.Assignment.GradeCategory.Name,
                        Weight = s.Assignment.GradeCategory.Weight, // Trọng số tại thời điểm này
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

        //public async Task<Guid> CreateReportAsync(CreateProgressReportDto request)
        //{
        //    var exist = await reportRepository.IsReportExistAsync(request.StudentId, request.ClassId, request.PeriodMonth, request.PeriodYear);
        //    if (exist) throw new Exception("Báo cáo tháng này của học sinh đã tồn tại.");

        //    var startDateDt = new DateTime(request.PeriodYear, request.PeriodMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        //    var endDateDt = new DateTime(request.PeriodYear, request.PeriodMonth, DateTime.DaysInMonth(request.PeriodYear, request.PeriodMonth), 23, 59, 59, DateTimeKind.Utc);

        //    var subs = await reportRepository.GetSubmissionsForCalcAsync(request.ClassId, startDateDt, endDateDt);
        //    var atts = await reportRepository.GetAttendancesForCalcAsync(request.ClassId, DateOnly.FromDateTime(startDateDt), DateOnly.FromDateTime(endDateDt));

        //    decimal gpa = CalculateGpa(subs.Where(s => s.StudentId == request.StudentId).ToList());
        //    decimal attRate = CalculateAttendance(atts.Where(a => a.StudentId == request.StudentId).ToList());

        //    var report = new ProgressReport
        //    {
        //        ReportId = Guid.NewGuid(),
        //        StudentId = request.StudentId,
        //        ClassId = request.ClassId,
        //        TeacherId = currentUserService.UserId,
        //        PeriodMonth = (short)request.PeriodMonth, 
        //        PeriodYear = request.PeriodYear,
        //        Title = request.Title,
        //        Content = request.Content,
        //        Status = request.Status,
        //        Gpa = gpa,
        //        AttendanceRate = attRate,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    await reportRepository.AddAsync(report);
        //    return report.ReportId;
        //}
        public async Task<Guid> CreateReportAsync(CreateProgressReportDto request)
        {
            // 1. Kiểm tra thời gian: Không cho tạo báo cáo cho tương lai
            var now = DateTime.UtcNow;
            if (request.PeriodYear > now.Year || (request.PeriodYear == now.Year && request.PeriodMonth > now.Month))
                throw new BadRequestException("Không thể tạo báo cáo cho tháng trong tương lai.");

            // 2. Kiểm tra trạng thái lớp
            var currentClass = await reportRepository.GetClassByIdAsync(request.ClassId);
            ValidateClassStatus(currentClass, request.ClassId);

            // 3. Kiểm tra tính đầy đủ: Nếu trạng thái là Ready thì BẮT BUỘC phải có nhận xét
            if (request.Status == "Ready" && string.IsNullOrWhiteSpace(request.Content))
                throw new BadRequestException("Vui lòng nhập nội dung nhận xét (Content) trước khi đặt trạng thái Sẵn sàng (Ready).");

            // 4. Kiểm tra trùng lặp
            var exist = await reportRepository.IsReportExistAsync(request.StudentId, request.ClassId, request.PeriodMonth, request.PeriodYear);
            if (exist) throw new BadRequestException($"Báo cáo tháng {request.PeriodMonth}/{request.PeriodYear} của học sinh này đã tồn tại.");

            // 5. Chuẩn bị dữ liệu tính toán
            var startDate = new DateOnly(request.PeriodYear, request.PeriodMonth, 1);
            var endDate = new DateOnly(request.PeriodYear, request.PeriodMonth, DateTime.DaysInMonth(request.PeriodYear, request.PeriodMonth));

            var startDateDt = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var endDateDt = endDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

            var subs = await reportRepository.GetSubmissionsForCalcAsync(request.ClassId, startDateDt, endDateDt);
            var atts = await reportRepository.GetAttendancesForCalcAsync(request.ClassId, startDate, endDate);

            // Lấy tổng số buổi dạy thực tế của lớp trong khoảng thời gian này
            int totalSessions = await reportRepository.GetTotalSessionsInPeriodAsync(request.ClassId, startDate, endDate);

            // 6. Tính toán điểm thực tế
            decimal gpa = CalculateGpa(subs.Where(s => s.StudentId == request.StudentId).ToList());
            decimal attRate = CalculateAttendance(atts.Where(a => a.StudentId == request.StudentId).ToList(), totalSessions);

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
                Gpa = gpa,
                AttendanceRate = attRate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await reportRepository.AddAsync(report);
            return report.ReportId;
        }

        //public async Task UpdateReportAsync(Guid id, UpdateProgressReportDto request)
        //{
        //    var report = await reportRepository.GetByIdAsync(id);
        //    if (report == null) throw new Exception("Không tìm thấy báo cáo.");
        //    if (report.TeacherId != currentUserService.UserId) throw new Exception("Bạn không có quyền sửa.");
        //    if (report.Status == "Published") throw new Exception("Báo cáo đã gửi không thể chỉnh sửa.");

        //    var startDateDt = new DateTime(report.PeriodYear!.Value, (int)report.PeriodMonth!.Value, 1, 0, 0, 0, DateTimeKind.Utc);

        //    var endDateDt = new DateTime(
        //        report.PeriodYear!.Value,
        //        (int)report.PeriodMonth!.Value,
        //        DateTime.DaysInMonth(report.PeriodYear!.Value, (int)report.PeriodMonth!.Value),
        //        23, 59, 59,
        //        DateTimeKind.Utc);

        //    var subs = await reportRepository.GetSubmissionsForCalcAsync(report.ClassId, startDateDt, endDateDt);
        //    var atts = await reportRepository.GetAttendancesForCalcAsync(report.ClassId, DateOnly.FromDateTime(startDateDt), DateOnly.FromDateTime(endDateDt));

        //    report.Title = request.Title;
        //    report.Content = request.Content;
        //    report.Status = request.Status;
        //    report.Gpa = CalculateGpa(subs.Where(s => s.StudentId == report.StudentId).ToList());
        //    report.AttendanceRate = CalculateAttendance(atts.Where(a => a.StudentId == report.StudentId).ToList());
        //    report.UpdatedAt = DateTime.UtcNow;

        //    await reportRepository.UpdateAsync(report);
        //}
        public async Task UpdateReportAsync(Guid id, UpdateProgressReportDto request)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new NotFoundException("Báo cáo", id);

            if (report.TeacherId != currentUserService.UserId) throw new ForbiddenAccessException();
            if (report.Status == "Published") throw new BadRequestException("Báo cáo đã gửi không thể chỉnh sửa.");

            // Kiểm tra nội dung khi giáo viên chuyển từ Draft sang Ready
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
        //public async Task SendReportAsync(Guid id)
        //{
        //    var report = await reportRepository.GetByIdAsync(id);
        //    if (report == null) throw new Exception("Không tìm thấy báo cáo.");
        //    if (report.TeacherId != currentUserService.UserId) throw new Exception("Bạn không có quyền.");
        //    if (report.Status == "Published") return;

        //    report.Status = "Published";
        //    report.UpdatedAt = DateTime.UtcNow;
        //    await reportRepository.UpdateAsync(report);

        //    //Notification
        //    try
        //    {
        //        if (report.Student != null)
        //        {
        //            string monthYear = $"{report.PeriodMonth}/{report.PeriodYear}";

        //            await _notificationService.SendNotificationAsync(
        //                targetAccountId: report.Student.AccountId,
        //                studentId: report.StudentId,
        //                title: "Báo cáo học tập mới",
        //                content: $"Báo cáo kết quả học tập tháng {monthYear} của lớp {report.Class.ClassName} đã có. Phụ huynh và học sinh vui lòng vào xem chi tiết.",
        //                actionUrl: $"/student/reports/{report.ReportId}",
        //                type: "Report"
        //            );
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Lỗi gửi thông báo Progress Report: {ex.Message}");
        //    }
        //}
        public async Task SendReportAsync(Guid id)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new NotFoundException("Báo cáo", id);
            if (report.TeacherId != currentUserService.UserId) throw new ForbiddenAccessException();
            if (report.Status == "Published") return;

            report.Status = "Published";
            report.UpdatedAt = DateTime.UtcNow;
            await reportRepository.UpdateAsync(report);

            // Gửi Notification
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
            if (report.TeacherId != currentUserService.UserId) throw new ForbiddenAccessException();

            // Chỉ cho xóa nháp, đã gửi rồi không được xóa để tránh mất dấu vết
            if (report.Status == "Published")
                throw new BadRequestException("Không thể xóa báo cáo đã gửi cho phụ huynh.");

            await reportRepository.DeleteAsync(report);
        }
        //public async Task DeleteReportAsync(Guid id)
        //{
        //    var report = await reportRepository.GetByIdAsync(id);
        //    if (report == null) throw new Exception("Không tìm thấy.");
        //    if (report.TeacherId != currentUserService.UserId) throw new Exception("Bạn không có quyền xóa.");
        //    if (report.Status == "Published") throw new Exception("Không thể xóa báo cáo đã gửi.");

        //    await reportRepository.DeleteAsync(report);
        //}

        //public async Task<ProgressReportResponseDto> GetReportDetailAsync(Guid id)
        //{
        //    var report = await reportRepository.GetByIdAsync(id);
        //    if (report == null) throw new Exception("Không tìm thấy báo cáo.");

        //    return new ProgressReportResponseDto
        //    {
        //        ReportId = report.ReportId,
        //        StudentId = report.StudentId,
        //        StudentName = report.Student?.FullName ?? "Unknown",
        //        ClassId = report.ClassId,
        //        TeacherId = report.TeacherId,
        //        PeriodMonth = report.PeriodMonth,
        //        PeriodYear = report.PeriodYear,
        //        Title = report.Title,
        //        Content = report.Content,
        //        Status = report.Status ?? "Draft",
        //        Gpa = report.Gpa ?? 0m,
        //        AttendanceRate = report.AttendanceRate ?? 0m,
        //        CreatedAt = report.CreatedAt,
        //        UpdatedAt = report.UpdatedAt
        //    };
        //}
        public async Task<ProgressReportResponseDto> GetReportDetailAsync(Guid id)
        {
            var report = await reportRepository.GetByIdAsync(id);
            if (report == null) throw new NotFoundException("Báo cáo", id);

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
                // Nếu đã Published thì lấy số đã chốt, nếu chưa thì lấy Live số mới nhất
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
