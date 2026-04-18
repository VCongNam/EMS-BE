using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reports.DTOs
{
    public class TeacherGrowthReportResponse
    {
        public Guid TeacherId { get; set; }
        public string Period { get; set; } = string.Empty;

        public OverviewMetrics TotalOverview { get; set; } = new();
        public StudentGrowthMetrics TotalStudentGrowth { get; set; } = new();
        public AcademicPerformanceMetrics TotalAcademicPerformance { get; set; } = new();

        public List<ClassBreakdownDto> ClassBreakdowns { get; set; } = new();
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
    }

    public class OverviewMetrics
    {
        public int TotalActiveStudents { get; set; }
        public double CapacityUtilizationPercent { get; set; }
        public short? MaxStudents { get; set; }
    }

    public class StudentGrowthMetrics
    {
        public int NewEnrollments { get; set; }
        public int Dropouts { get; set; }
        public int NetGrowth => NewEnrollments - Dropouts;
    }

    public class AcademicPerformanceMetrics
    {
        public double AttendanceRatePercent { get; set; }
        public GradingDistributionDto Grading { get; set; } = new();
    }

    public class GradingDistributionDto
    {
        public int ExcellentCount { get; set; } // Giỏi (>= 8.0)
        public int GoodCount { get; set; }      // Khá (6.5 - 7.9)
        public int AverageCount { get; set; }   // Trung bình (5.0 - 6.4)
        public int WeakCount { get; set; }      // Yếu (< 5.0)

        public int TotalGradedStudents => ExcellentCount + GoodCount + AverageCount + WeakCount;
        public int AboveAverageCount => ExcellentCount + GoodCount + AverageCount; // Điểm >= 5.0
        public double AboveAveragePercent => TotalGradedStudents > 0
            ? Math.Round((double)AboveAverageCount / TotalGradedStudents * 100, 2)
            : 0;
    }
}
