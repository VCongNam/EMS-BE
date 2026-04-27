using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.SystemAdmin.Dtos
{
    public class DashboardFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalActiveClasses { get; set; }

        public int NewRegistrationsInPeriod { get; set; }
        public int EngagementInPeriod { get; set; }

        public List<ChartDataDto> UserGrowthChart { get; set; } = new();
        public List<ChartDataDto> SystemUsageChart { get; set; } = new();
    }

    public class ChartDataDto
    {
        public string Label { get; set; } = string.Empty;
        public int Value1 { get; set; } // Ví dụ: Teacher / Post
        public int Value2 { get; set; } // Ví dụ: Student / Assignment
    }

    public class TeacherGridDto
    {
        public Guid TeacherId { get; set; }
        public string? AvatarUrl { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? JoinedDate { get; set; }
        public int ActiveClassesCount { get; set; }
        public int TotalStudentsCount { get; set; }
    }

    public class TeacherDetailDto
    {
        public Guid TeacherId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Specialization { get; set; }
        public List<TeacherClassDto> CurrentClasses { get; set; } = new();
    }

    public class TeacherClassDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
