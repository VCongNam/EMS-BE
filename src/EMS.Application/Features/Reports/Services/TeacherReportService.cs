using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Reports.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reports.Services
{
    public class TeacherReportService : ITeacherReportService
    {
        private readonly ITeacherReportRepository reportRepository;
        private readonly ICurrentUserService currentUserService;

        public TeacherReportService(ITeacherReportRepository reportRepository, ICurrentUserService currentUserService)
        {
            this.reportRepository = reportRepository;
            this.currentUserService = currentUserService;
        }

        private decimal CalculateGpa(List<Submission> subs)
        {
            if (!subs.Any()) return 0m;
            decimal totalWeighted = subs.Sum(s => (s.Grade ?? 0m) * (s.Assignment.GradeCategory.Weight / 100m));
            decimal totalWeight = subs.Sum(s => s.Assignment.GradeCategory.Weight / 100m);
            return totalWeight > 0 ? Math.Round(totalWeighted / totalWeight, 2) : 0m;
        }

        // Đã sửa tham số đầu vào sang List Tuple để khớp với Repository
        private List<EnrollmentTrendDto> FillMissingDates(List<(DateOnly Date, int Count)> rawData, DateOnly start, DateOnly end)
        {
            var filledList = new List<EnrollmentTrendDto>();
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                var existing = rawData.FirstOrDefault(x => x.Date == date);
                filledList.Add(new EnrollmentTrendDto
                {
                    Date = date,
                    Count = existing.Date != default ? existing.Count : 0
                });
            }
            return filledList;
        }

        public async Task<TeacherGrowthReportResponse> GetGrowthReportAsync(DateTime startDate, DateTime endDate, Guid? subjectId, string? status)
        {
            if (startDate >= endDate) throw new BadRequestException("Ngày bắt đầu phải trước ngày kết thúc.");

            var teacherId = currentUserService.UserId;
            var start = DateOnly.FromDateTime(startDate);
            var end = DateOnly.FromDateTime(endDate);

            var classes = await reportRepository.GetFilteredClassesAsync(teacherId, start, end, subjectId, status);
            var response = new TeacherGrowthReportResponse { TeacherId = teacherId, Period = $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}" };

            if (!classes.Any()) return response;

            var classIds = classes.Select(c => c.ClassId).ToList();
            var enrollStats = await reportRepository.GetEnrollmentStatsAsync(classIds, start, end);
            var attendStats = await reportRepository.GetAttendanceStatsAsync(classIds, start, end);
            var allSubmissions = await reportRepository.GetSubmissionsForClassesAsync(classIds, startDate, endDate);

            var rawTrend = await reportRepository.GetEnrollmentTrendAsync(classIds, start, end);
            response.GlobalEnrollmentTrend = FillMissingDates(rawTrend, start, end);

            int totalNew = 0, totalDrop = 0, totalPresent = 0, totalSlots = 0, totalStudents = 0, totalMax = 0;
            var totalGrading = new GradingDistributionDto();

            foreach (var cls in classes)
            {
                // Sửa logic GetValueOrDefault để có tên cho Tuple
                var eStat = enrollStats.GetValueOrDefault(cls.ClassId, (NewCount: 0, DropoutCount: 0));
                var aStat = attendStats.GetValueOrDefault(cls.ClassId, (TotalSlots: 0, PresentCount: 0));

                int activeInClass = cls.ClassEnrollments.Count(x => x.Status == "Active");

                var classGrading = new GradingDistributionDto();
                var classSubs = allSubmissions.Where(s => s.Assignment.ClassId == cls.ClassId).GroupBy(s => s.StudentId);

                foreach (var studentGroup in classSubs)
                {
                    decimal gpa = CalculateGpa(studentGroup.ToList());
                    if (gpa >= 8.0m) { classGrading.ExcellentCount++; totalGrading.ExcellentCount++; }
                    else if (gpa >= 6.5m) { classGrading.GoodCount++; totalGrading.GoodCount++; }
                    else if (gpa >= 5.0m) { classGrading.AverageCount++; totalGrading.AverageCount++; }
                    else { classGrading.WeakCount++; totalGrading.WeakCount++; }
                }

                response.ClassBreakdowns.Add(new ClassBreakdownDto
                {
                    ClassId = cls.ClassId,
                    ClassName = cls.ClassName,
                    SubjectName = cls.Subject?.SubjectName ?? "N/A",
                    Status = cls.Status ?? "N/A",
                    Overview = new OverviewMetrics { TotalActiveStudents = activeInClass, MaxStudents = cls.MaxStudents, CapacityUtilizationPercent = cls.MaxStudents > 0 ? Math.Round((double)activeInClass / cls.MaxStudents.Value * 100, 2) : 0 },
                    StudentGrowth = new StudentGrowthMetrics { NewEnrollments = eStat.NewCount, Dropouts = eStat.DropoutCount },
                    AcademicPerformance = new AcademicPerformanceMetrics { AttendanceRatePercent = aStat.TotalSlots > 0 ? Math.Round((double)aStat.PresentCount / aStat.TotalSlots * 100, 2) : 0, Grading = classGrading }
                });

                totalNew += eStat.NewCount; totalDrop += eStat.DropoutCount; totalPresent += aStat.PresentCount;
                totalSlots += aStat.TotalSlots; totalStudents += activeInClass; totalMax += cls.MaxStudents ?? 0;
            }

            // ... (Phần map response tổng kết giữ nguyên)
            response.TotalOverview = new OverviewMetrics { TotalActiveStudents = totalStudents, CapacityUtilizationPercent = totalMax > 0 ? Math.Round((double)totalStudents / totalMax * 100, 2) : 0 };
            response.TotalStudentGrowth = new StudentGrowthMetrics { NewEnrollments = totalNew, Dropouts = totalDrop };
            response.TotalAcademicPerformance = new AcademicPerformanceMetrics { AttendanceRatePercent = totalSlots > 0 ? Math.Round((double)totalPresent / totalSlots * 100, 2) : 0, Grading = totalGrading };

            return response;
        }

        public async Task<ClassBreakdownDto> GetSingleClassGrowthReportAsync(Guid classId, DateTime startDate, DateTime endDate)
        {
            var cls = await reportRepository.GetClassByIdAsync(classId);
            if (cls == null) throw new NotFoundException("Lớp học", classId);

            var teacherId = currentUserService.UserId;
            if (cls.TeacherId != teacherId) throw new ForbiddenAccessException("Không có quyền truy cập.");

            var startOnly = DateOnly.FromDateTime(startDate);
            var endOnly = DateOnly.FromDateTime(endDate);
            var classIds = new List<Guid> { classId };

            var enrollStats = await reportRepository.GetEnrollmentStatsAsync(classIds, startOnly, endOnly);
            var attendStats = await reportRepository.GetAttendanceStatsAsync(classIds, startOnly, endOnly);
            var submissions = await reportRepository.GetSubmissionsForClassesAsync(classIds, startDate, endDate);
            var rawTrend = await reportRepository.GetEnrollmentTrendAsync(classIds, startOnly, endOnly);

            var classGrading = new GradingDistributionDto();
            var studentGradeList = new List<StudentGradeSummaryDto>();
            var studentGroups = submissions.GroupBy(s => s.StudentId);

            foreach (var group in studentGroups)
            {
                var studentId = group.Key;
                var studentName = cls.ClassEnrollments.FirstOrDefault(e => e.StudentId == studentId)?.Student?.FullName ?? "Học sinh ẩn danh";
                decimal gpa = CalculateGpa(group.ToList());
                string rank = gpa >= 8.0m ? "Giỏi" : gpa >= 6.5m ? "Khá" : gpa >= 5.0m ? "Trung bình" : "Yếu";

                if (gpa >= 8.0m) classGrading.ExcellentCount++;
                else if (gpa >= 6.5m) classGrading.GoodCount++;
                else if (gpa >= 5.0m) classGrading.AverageCount++;
                else classGrading.WeakCount++;

                studentGradeList.Add(new StudentGradeSummaryDto { StudentId = studentId, StudentName = studentName, Gpa = gpa, Rank = rank });
            }

            var eStat = enrollStats.GetValueOrDefault(classId, (NewCount: 0, DropoutCount: 0));
            var aStat = attendStats.GetValueOrDefault(classId, (TotalSlots: 0, PresentCount: 0));
            int activeInClass = cls.ClassEnrollments.Count(x => x.Status == "Active");

            return new ClassBreakdownDto
            {
                ClassId = cls.ClassId,
                ClassName = cls.ClassName,
                SubjectName = cls.Subject?.SubjectName ?? "N/A",
                Status = cls.Status ?? "N/A",
                Overview = new OverviewMetrics { TotalActiveStudents = activeInClass, MaxStudents = cls.MaxStudents, CapacityUtilizationPercent = cls.MaxStudents > 0 ? Math.Round((double)activeInClass / cls.MaxStudents.Value * 100, 2) : 0 },
                StudentGrowth = new StudentGrowthMetrics { NewEnrollments = eStat.NewCount, Dropouts = eStat.DropoutCount },
                AcademicPerformance = new AcademicPerformanceMetrics { AttendanceRatePercent = aStat.TotalSlots > 0 ? Math.Round((double)aStat.PresentCount / aStat.TotalSlots * 100, 2) : 0, Grading = classGrading },
                StudentGrades = studentGradeList.OrderByDescending(x => x.Gpa).ToList(),
                EnrollmentTrend = FillMissingDates(rawTrend, startOnly, endOnly)
            };
        }

    }
}
