using EMS.Application.Common.Helpers;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Students.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.Services
{
    public class StudentAssignmentService : IStudentAssignmentService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ISupabaseStorageService _supabaseStorageService;
        private readonly ISubmissionRepository _submissionRepository;
        public StudentAssignmentService(ICurrentUserService currentUser, IAssignmentRepository assignmentRepository, ISupabaseStorageService supabaseStorageService, ISubmissionRepository submissionRepository)
        {
            _currentUser = currentUser;
            _assignmentRepository = assignmentRepository;
            _supabaseStorageService = supabaseStorageService;
            _submissionRepository = submissionRepository;
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

        public async Task<bool> SubmitAssignmentAsync(Guid assignmentId, SubmitAssignmentRequest request)
        {
            Guid studentId = _currentUser.UserId;

            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null)
            {
                throw new Exception("Bài tập không tồn tại!");
            }
            if(assignment.DueDate < DateTime.UtcNow && assignment.AllowLateSubmission == false)
            {
                throw new Exception("Đã hết hạn nộp bài");
            }

            foreach(var file in request.Files)
            {
                DataValidator.ValidateFile(file);
            }

            var newAttachment = new List<SubmissionAttachment>();
            foreach(var file in request.Files)
            {
                string fileUrl = await _supabaseStorageService.UploadFileAsync(file, "submissions");
                newAttachment.Add(new SubmissionAttachment
                {
                    AttachmentId = Guid.NewGuid(),
                    FileUrl = fileUrl,
                    FileName = file.FileName,
                    FileType = Path.GetExtension(file.FileName),
                    FileSize = file.Length,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            var existingSubmission = await _submissionRepository.GetSubmissionWithAttachmentsAsync(assignmentId, studentId);
            if(existingSubmission == null)
            {
                var newSubmission = new Submission
                {
                    SubmissionId = Guid.NewGuid(),
                    AssignmentId = assignmentId,
                    StudentId = studentId,
                    SubmittedAt = DateTime.UtcNow,
                    Status = "Submitted",
                    SubmissionAttachments = newAttachment
                };
                await _submissionRepository.AddAsync(newSubmission);
            } 
            else
            {
                if (existingSubmission.Grade.HasValue)
                {
                    throw new Exception("Bài tập đã được chấm, không thể nộp lại!");
                }

                var oldFileUrls = existingSubmission.SubmissionAttachments
                    .Select(x => x.FileUrl)
                    .ToList();
                // Xóa url cũ
                existingSubmission.SubmissionAttachments.Clear();
                foreach(var attachment in newAttachment)
                {
                    existingSubmission.SubmissionAttachments.Add(attachment);
                }
                existingSubmission.SubmittedAt = DateTime.UtcNow;
                await _submissionRepository.UpdateAsync(existingSubmission);
                // xóa file cũ trong storage
                var deleteTasks = oldFileUrls.Select(url => _supabaseStorageService.DeleteFileByUrlAsync(url));
                await Task.WhenAll(deleteTasks);
            }

            return true;
        }
    }
}
