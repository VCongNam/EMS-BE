using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reports.DTOs
{
    public class EnrollmentTrendDto
    {
        public DateOnly Date { get; set; }
        public int Count { get; set; }
    }

    public class GradingDistributionDto
    {
        public int ExcellentCount { get; set; } // >= 8.0
        public int GoodCount { get; set; }      // >= 6.5
        public int AverageCount { get; set; }   // >= 5.0
        public int WeakCount { get; set; }      // < 5.0
    }

    public class OverviewMetrics
    {
        public int TotalActiveStudents { get; set; }
        public int? MaxStudents { get; set; }
        public double CapacityUtilizationPercent { get; set; }
    }

    public class StudentGrowthMetrics
    {
        public int NewEnrollments { get; set; }
        public int Dropouts { get; set; }
    }

    public class AcademicPerformanceMetrics
    {
        public double AttendanceRatePercent { get; set; }
        public GradingDistributionDto Grading { get; set; } = new();
    }

    public class StudentGradeSummaryDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public decimal Gpa { get; set; }
        public string Rank { get; set; } = string.Empty;
    }

    public class ClassBreakdownDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public OverviewMetrics Overview { get; set; } = new();
        public StudentGrowthMetrics StudentGrowth { get; set; } = new();
        public AcademicPerformanceMetrics AcademicPerformance { get; set; } = new();

        // Dữ liệu cho biểu đồ Line Chart của riêng lớp này
        public List<EnrollmentTrendDto> EnrollmentTrend { get; set; } = new();

        // Danh sách điểm học sinh (chỉ dùng cho báo cáo chi tiết 1 lớp)
        public List<StudentGradeSummaryDto>? StudentGrades { get; set; }
    }

    public class TeacherGrowthReportResponse
    {
        public Guid TeacherId { get; set; }
        public string Period { get; set; } = string.Empty;
        public OverviewMetrics TotalOverview { get; set; } = new();
        public StudentGrowthMetrics TotalStudentGrowth { get; set; } = new();
        public AcademicPerformanceMetrics TotalAcademicPerformance { get; set; } = new();

        // Dữ liệu biểu đồ tổng hợp cho tất cả các lớp của giáo viên
        public List<EnrollmentTrendDto> GlobalEnrollmentTrend { get; set; } = new();
        public List<ClassBreakdownDto> ClassBreakdowns { get; set; } = new();
    }
}
