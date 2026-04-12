using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assignments.DTOs
{
    public class AssignmentSubmissionsListDto
    {
        public Guid AssignmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal MaxScore { get; set; }
        public bool IsOffline { get; set; }

        public List<StudentSubmissionDto> Students { get; set; } = new();
    }

    public class StudentSubmissionDto
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        // Thông tin bài nộp (Sẽ Null nếu học sinh chưa nộp)
        public Guid? SubmissionId { get; set; }
        public DateTime? SubmittedAt { get; set; }

        // Trạng thái hiển thị: "Chưa nộp", "Đã nộp", "Nộp muộn", "Đã chấm", "Thiếu bài"
        public string Status { get; set; } = string.Empty;
        public decimal? Grade { get; set; }
    }

}
