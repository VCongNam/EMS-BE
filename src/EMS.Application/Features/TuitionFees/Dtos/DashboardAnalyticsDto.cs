using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class DashboardAnalyticsDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalStudents { get; set; }
        public decimal AverageRevenuePerClass { get; set; }
        public decimal QuarterlyTarget { get; set; }
        public List<RevenueTrendDto> RevenueTrends { get; set; } = new();
        public List<ClassRevenueDistributionDto> RevenueByClasses { get; set; } = new();
    }
}
