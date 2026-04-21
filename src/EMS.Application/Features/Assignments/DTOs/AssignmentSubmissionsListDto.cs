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
        public List<SubmissionFileDto> Attachments { get; set; } = new();
        public List<SubmissionFileDto> CorrectionFiles { get; set; } = new();

        public string Status { get; set; } = string.Empty;
        public string GradeStatus { get; set; } = string.Empty;
        public decimal? Grade { get; set; }
    }

    public class SubmissionFileDto
    {
        public Guid AttachmentId { get; set; }
        public string? FileName { get; set; }
        public string? FileUrl { get; set; }
        public string? FileType { get; set; }
        public long? FileSize { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
