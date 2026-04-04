using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.ProgressReports.DTOs
{
    public class ProgressReportDashboardDto
    {
        public int TotalClasses { get; set; }
        public double OverallCompletionRate { get; set; } // Tỷ lệ hoàn thành tổng
        public List<ClassReportSummaryItemDto> ClassSummaries { get; set; } = new List<ClassReportSummaryItemDto>();
    }
}
