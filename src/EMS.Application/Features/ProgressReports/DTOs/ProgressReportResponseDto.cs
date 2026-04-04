using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.ProgressReports.DTOs
{
    public class ProgressReportResponseDto
    {
        public Guid? ReportId { get; set; } // Nullable vì có thể học sinh chưa có báo cáo (trạng thái Sẵn sàng)
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = null!;

        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string Status { get; set; }

        // Bổ sung điểm số giả định để render UI Màn hình 2
        public double? Gpa { get; set; }
        public double? AttendanceRate { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }    

    }
}
