using DocumentFormat.OpenXml.Spreadsheet;
using EMS.Application.Common.Helpers;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Notifications.Services;
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
        private readonly INotificationService _notificationService;
        public StudentAssignmentService(
            ICurrentUserService currentUser, 
            IAssignmentRepository assignmentRepository, 
            ISupabaseStorageService supabaseStorageService, 
            ISubmissionRepository submissionRepository,
            INotificationService notificationService)
        {
            _currentUser = currentUser;
            _assignmentRepository = assignmentRepository;
            _supabaseStorageService = supabaseStorageService;
            _submissionRepository = submissionRepository;
            _notificationService = notificationService;
        }

        public async Task<PagedResult<AssignmentItemDto>> GetClassAssignmentsAsync(Guid classId, AssignmentFilter filter)
        {
            Guid studentId = _currentUser.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");
            if(classId == null)
            {
                throw new Exception("ClassId không được để trống");
            }
            var (assignments, totalCount) = await _assignmentRepository
                .GetStudentAssignmentsAsync(classId, studentId, filter.Page, filter.Size);
            var now = DateTime.UtcNow;

            var items = assignments.Select(a =>
            {
                var s = a.Submissions.FirstOrDefault();

                bool isSubmitted = s?.SubmittedAt != null;

                string status;
                if (isSubmitted)
                {
                    status = s!.Grade.HasValue ? "Đã chấm" : "Đã nộp";
                }
                else if (a.DueDate < now)
                {
                    status = "Quá hạn";
                }
                else
                {
                    status = "Chưa nộp";
                }

                return new AssignmentItemDto
                {
                    AssignmentID = a.AssignmentId,
                    Title = a.Title,
                    DueDate = a.DueDate,
                    IsSubmitted = isSubmitted,
                    SubmittedAt = s?.SubmittedAt,
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
            Guid studentId = _currentUser.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");

            var (assignment, submission) = await _assignmentRepository.GetAssignmentDetailAsync(assignmentId, studentId);
            if (assignment == null)
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
            Guid studentId = _currentUser.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");

            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null)
            {
                throw new Exception("Bài tập không tồn tại!");
            }
            if (assignment.DueDate < DateTime.UtcNow && assignment.AllowLateSubmission == false)
            {
                throw new Exception("Đã hết hạn nộp bài");
            }

            foreach (var file in request.Files)
            {
                DataValidator.ValidateFile(file);
            }

            //New submission
            var existingSubmission = await _submissionRepository.GetSubmissionWithAttachmentsAsync(assignmentId, studentId);
            if (existingSubmission == null)
            {
                var newSubmission = new Submission
                {
                    SubmissionId = Guid.NewGuid(),
                    AssignmentId = assignmentId,
                    StudentId = studentId,
                    SubmittedAt = DateTime.UtcNow,
                    Status = "Submitted",
                    SubmissionAttachments = new List<SubmissionAttachment>()
                };
                foreach (var file in request.Files)
                {
                    string fileUrl = await _supabaseStorageService.UploadFileAsync(file, "submissions");
                    newSubmission.SubmissionAttachments.Add(new SubmissionAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        SubmissionId = newSubmission.SubmissionId,
                        FileUrl = fileUrl,
                        FileName = file.FileName,
                        FileType = Path.GetExtension(file.FileName),
                        FileSize = file.Length,
                        CreatedAt = DateTime.UtcNow,
                    });
                }
                await _submissionRepository.AddAsync(newSubmission);
            }
            else
            {
                if (existingSubmission.Grade.HasValue)
                {
                    throw new Exception("Bài tập đã được chấm, không thể nộp lại!");
                }
                // Update assignment
                var oldAttachments = existingSubmission.SubmissionAttachments.ToList();
                var oldFileUrls = oldAttachments.Select(a => a.FileUrl).ToList();

                await _submissionRepository.DeleteSubmissionAttachmentsAsync(oldAttachments);

                var newAttachments = new List<SubmissionAttachment>();
                foreach (var file in request.Files)
                {
                    string fileUrl = await _supabaseStorageService.UploadFileAsync(file, "submissions");
                    newAttachments.Add(new SubmissionAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        SubmissionId = existingSubmission.SubmissionId, // Gắn ID bài nộp vào
                        FileUrl = fileUrl,
                        FileName = file.FileName,
                        FileType = Path.GetExtension(file.FileName),
                        FileSize = file.Length,
                        CreatedAt = DateTime.UtcNow,
                    });
                }
                await _submissionRepository.AddAttachmentsAsync(newAttachments);
                existingSubmission.SubmittedAt = DateTime.UtcNow;

                await _submissionRepository.UpdateAsync(existingSubmission);

                var deleteTasks = oldFileUrls.Select(url => _supabaseStorageService.DeleteFileByUrlAsync(url));
                await Task.WhenAll(deleteTasks);
            }

            //Notification
            var asignmentInfo = await _assignmentRepository.GetWithClassByIdAsync(assignmentId);
            if (asignmentInfo != null)
            {
                await _notificationService.SendNotificationAsync(
                    targetAccountId: asignmentInfo.Class.TeacherId,
                    studentId: studentId,
                    title: "Bài nộp mới",
                    content: $"Học sinh đã nộp bài tập: {asignmentInfo.Title}",
                    actionUrl: $"/teacher/classes/{asignmentInfo.ClassId}/assignment/{asignmentInfo.AssignmentId}",
                    type: "Submission");
            }

            return true;
        }

        public async Task<bool> UnsubmitAssignmentAsync(Guid assignmentId)
        {
            Guid studentId = _currentUser.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");
            var existingSubmission = await _submissionRepository.GetSubmissionWithAttachmentsAsync(assignmentId, studentId);
            if (existingSubmission == null)
            {
                throw new Exception("Bạn chưa nộp bài tập này nên không thể hủy.");
            }
            if (existingSubmission.Grade.HasValue)
            {
                throw new Exception("Bài tập đã được chấm điểm, không thể hủy nộp bài.");
            }

            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment != null && assignment.DueDate < DateTime.UtcNow)
            { 
                if ((bool)!assignment.AllowLateSubmission)
                {
                    throw new Exception("Đã quá hạn nộp bài. Việc hủy bài lúc này sẽ khiến bạn không thể nộp lại được nữa!");
                }
            }
            var fileUrlsToDelete = existingSubmission.SubmissionAttachments?
                .Select(a => a.FileUrl)
                .ToList() ?? new List<string>();

            if (existingSubmission.SubmissionAttachments != null && existingSubmission.SubmissionAttachments.Any())
            {
                await _submissionRepository.DeleteSubmissionAttachmentsAsync(existingSubmission.SubmissionAttachments);
            }
            await _submissionRepository.DeleteSubmissionAsync(existingSubmission);
            if (fileUrlsToDelete.Any())
            {
                var deleteTasks = fileUrlsToDelete.Select(url => _supabaseStorageService.DeleteFileByUrlAsync(url));
                await Task.WhenAll(deleteTasks);
            }

            return true;
        }
    }
}
