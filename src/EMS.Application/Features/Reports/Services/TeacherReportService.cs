using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Reports.DTOs;
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

        public async Task<TeacherGrowthReportResponse> GetGrowthReportAsync(DateTime startDate, DateTime endDate)
        {
            // 1. VALIDATION NGHIỆP VỤ (Business Rules)
            // Middleware sẽ bắt lỗi này và tự động trả về HTTP 400
            if (startDate >= endDate)
            {
                throw new BadRequestException("Ngày bắt đầu báo cáo phải diễn ra trước ngày kết thúc.");
            }

            // 2. LẤY THÔNG TIN NGƯỜI DÙNG HIỆN TẠI (Tránh IDOR)
            var teacherId = currentUserService.UserId;
            var start = DateOnly.FromDateTime(startDate);
            var end = DateOnly.FromDateTime(endDate);

            // 3. LẤY DỮ LIỆU TỪ REPOSITORY
            var classes = await reportRepository.GetActiveClassesAsync(teacherId);

            // Kiểm tra nếu giáo viên chưa có lớp nào
            // Middleware sẽ bắt lỗi này và tự trả về HTTP 404
            if (classes == null || !classes.Any())
            {
                throw new NotFoundException("Giáo viên này hiện không có lớp học nào đang hoạt động trong hệ thống.");
            }

            var classIds = classes.Select(c => c.ClassId).ToList();

            var enrollStats = await reportRepository.GetEnrollmentStatsAsync(classIds, start, end);
            var attendStats = await reportRepository.GetAttendanceStatsAsync(classIds, start, end);

            // 4. KHỞI TẠO DTO RESPONSE
            var response = new TeacherGrowthReportResponse
            {
                TeacherId = teacherId,
                Period = $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}"
            };

            int totalNew = 0, totalDrop = 0, totalPresent = 0, totalSlots = 0, totalStudents = 0, totalMax = 0;

            // 5. MAP DỮ LIỆU VÀ TÍNH TOÁN (Cho từng lớp và Tổng)
            foreach (var cls in classes)
            {
                // Lấy data từ Dictionary, nếu không có thì trả về 0
                var eStat = enrollStats.GetValueOrDefault(cls.ClassId, (NewCount: 0, DropoutCount: 0));
                var aStat = attendStats.GetValueOrDefault(cls.ClassId, (TotalSlots: 0, PresentCount: 0));

                int activeInClass = cls.ClassEnrollments.Count(x => x.Status == "Active");

                // Tính tỉ lệ riêng cho từng lớp
                double classCapacity = cls.MaxStudents > 0
                    ? Math.Round((double)activeInClass / cls.MaxStudents.Value * 100, 2)
                    : 0;

                double classAttendance = aStat.TotalSlots > 0
                    ? Math.Round((double)aStat.PresentCount / aStat.TotalSlots * 100, 2)
                    : 0;

                // Thêm vào danh sách Breakdown
                var breakdown = new ClassBreakdownDto
                {
                    ClassId = cls.ClassId,
                    ClassName = cls.ClassName,
                    SubjectName = cls.Subject?.SubjectName ?? "N/A",
                    Overview = new OverviewMetrics
                    {
                        TotalActiveStudents = activeInClass,
                        MaxStudents = cls.MaxStudents,
                        CapacityUtilizationPercent = classCapacity
                    },
                    StudentGrowth = new StudentGrowthMetrics
                    {
                        NewEnrollments = eStat.NewCount,
                        Dropouts = eStat.DropoutCount
                    },
                    AcademicPerformance = new AcademicPerformanceMetrics
                    {
                        AttendanceRatePercent = classAttendance
                    }
                };

                response.ClassBreakdowns.Add(breakdown);

                // Cộng dồn để tính số liệu Tổng
                totalNew += eStat.NewCount;
                totalDrop += eStat.DropoutCount;
                totalPresent += aStat.PresentCount;
                totalSlots += aStat.TotalSlots;
                totalStudents += activeInClass;
                totalMax += cls.MaxStudents ?? 0;
            }

            // 6. GÁN DỮ LIỆU TỔNG (Aggregate)
            response.TotalOverview = new OverviewMetrics
            {
                TotalActiveStudents = totalStudents,
                CapacityUtilizationPercent = totalMax > 0 ? Math.Round((double)totalStudents / totalMax * 100, 2) : 0
            };
            response.TotalStudentGrowth = new StudentGrowthMetrics
            {
                NewEnrollments = totalNew,
                Dropouts = totalDrop
            };
            response.TotalAcademicPerformance = new AcademicPerformanceMetrics
            {
                AttendanceRatePercent = totalSlots > 0 ? Math.Round((double)totalPresent / totalSlots * 100, 2) : 0
            };

            return response;
        }
    }
}
