using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.ProgressReports.DTOs
{
    public class ClassReportSummaryItemDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public string? Room { get; set; }

        // Phục vụ vẽ thanh Progress Bar chia phần
        public int TotalStudents { get; set; }
        public int DraftCount { get; set; }      // Số bản nháp (Màu vàng/cam)
        public int PublishedCount { get; set; }  // Số bản đã gửi (Màu xanh lá)
        public int ReadyCount => TotalStudents - DraftCount - PublishedCount; // Chưa làm (Màu xám)

        public int CreatedReports => DraftCount + PublishedCount; // Hiển thị kiểu "28 / 32"
        public double CompletionRate { get; set; } // Hiển thị "88%"

        // Phục vụ cảnh báo Deadline
        public DateTime Deadline { get; set; }
        public bool IsNearDeadline { get; set; } // Nếu True -> Giao diện tự bôi đỏ text

        public DateTime? LastUpdated { get; set; } // Hiển thị "Cập nhật 2 giờ trước"
    }
}
