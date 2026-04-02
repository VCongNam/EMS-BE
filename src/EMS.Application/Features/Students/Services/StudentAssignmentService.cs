using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Students.DTOs;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.Services
{
    public class StudentAssignmentService : IStudentAssignmentService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IAssignmentRepository _assignmentRepository;
        public StudentAssignmentService(ICurrentUserService currentUser, IAssignmentRepository assignmentRepository)
        {
            _currentUser = currentUser;
            _assignmentRepository = assignmentRepository;
        }

        public async Task<PagedResult<AssignmentItemDto>> GetClassAssignmentsAsync(Guid classId, AssignmentFilter filter)
        {
            Guid studentId = _currentUser.UserId;
            var (models, totalCount) = await _assignmentRepository.GetStudentAssignmentsAsync(classId, studentId, filter.Page, filter.Size);
            var items = models.Select(m =>
            {
                var a = m.Assignment;
                var s = m.Submission;
                string status = "Chưa nộp";
                if (s!=null)
                {
                    status = s.Grade.HasValue ? "Đã chấm" : "Đã Nộp";
                }
                if(s.SubmittedAt > a.DueDate){
                    status = "Quá hạn";
                }
                return new AssignmentItemDto
                {
                    AssignmentID = a.AssignmentId,
                    Title = a.Title,
                    DueDate = a.DueDate,
                    StudentStatus = status
                };
            }).ToList();
            return new PagedResult<AssignmentItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.Size),
                CurrentPage = filter.Page
            };
        }

        public async Task<AssignmentDetailDto> GetClassAssignmentsDetailAsync(Guid assignmentId)
        {
            Guid studentId = _currentUser.UserId;

            var (assignment, submission) = await _assignmentRepository.GetAssignmentDetailAsync(assignmentId, studentId);
            if(assignment == null)
            {
                throw new KeyNotFoundException("Không tìm thấy bài tập này!");
            }

            SubmissionDetailDto? submissionDto = null;
            if (submission != null)
            {
                submissionDto = new SubmissionDetailDto
                {
                    SubmissionID = submission.SubmissionId,
                    SubmittedAt = (DateTime)submission.SubmittedAt,
                    Grade = submission.Grade,
                    Status = submission.Status,

                    Attachments = submission.SubmissionAttachments?
                    .Select(sa => new AttachmentDto
                    {
                        AttachmentID = sa.AttachmentId,
                        FileName = sa.FileName,
                        FileURL = sa.FileUrl,
                        FileType = sa.FileType,
                        FileSize = sa.FileSize
                    }).ToList() ?? new List<AttachmentDto>(),

                    Feedbacks = submission.SubmissionFeedbacks?
                        .OrderBy(f => f.CreatedAt)
                        .Select(f => f.Content)
                        .ToList() ?? new List<string>()
                };
            }

            return new AssignmentDetailDto
            {
                AssignmentID = assignment.AssignmentId,
                Title = assignment.Title,
                Description = assignment.Description,
                DueDate = assignment.DueDate,

                // MỚI: Map mảng file đính kèm của đề bài
                Attachments = assignment.AssignmentAttachments?
            .Select(aa => new AttachmentDto
            {
                AttachmentID = aa.AttachmentId,
                FileName = aa.FileName,
                FileURL = aa.FileUrl,
                FileType = aa.FileType,
                FileSize = aa.FileSize
            }).ToList() ?? new List<AttachmentDto>(),

                MySubmission = submissionDto
            };
        }
    }
}
