using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Assignments.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assignments.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly ISupabaseStorageService _storageService;
        private readonly IClassRepository _classRepository;
        private readonly ICurrentUserService _currentUserService;

        // Giới hạn file: 10MB (theo cấu hình Supabase bucket)
        private const long MaxFileSize = 10 * 1024 * 1024;
        private static readonly string[] AllowedMimeTypes =
        {
            "image/png", "image/jpeg", "image/jpg", "image/gif", "image/webp", "image/svg+xml", "image/bmp",
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/zip",
            "application/x-rar-compressed"
        };

        public AssignmentService(
            IAssignmentRepository assignmentRepository,
            ISubmissionRepository submissionRepository,
            ISupabaseStorageService storageService,
            IClassRepository classRepository,
            ICurrentUserService currentUserService)
        {
            _assignmentRepository = assignmentRepository;
            _submissionRepository = submissionRepository;
            _storageService = storageService;
            _classRepository = classRepository;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> CreateAssignmentAsync(CreateAssignmentDto request)
        {
            var assignment = new Assignment
            {
                AssignmentId = Guid.NewGuid(),
                ClassId = request.ClassId,
                AuthorId = _currentUserService.UserId,
                GradeCategoryId = request.GradeCategoryId,
                Title = request.Title,
                Description = request.Description,
                DueDate = request.DueDate,
                AllowLateSubmission = request.AllowLateSubmission,
                Status = "Published",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await _assignmentRepository.AddAsync(assignment);

            // Upload attachments nếu có
            if (request.Attachments != null && request.Attachments.Count > 0)
            {
                foreach (var file in request.Attachments)
                {
                    ValidateFile(file.FileName, file.Length, file.ContentType);

                    var fileUrl = await _storageService.UploadFileAsync(file, $"assignments/{assignment.AssignmentId}");

                    var attachment = new AssignmentAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        AssignmentId = assignment.AssignmentId,
                        FileName = file.FileName,
                        FileUrl = fileUrl,
                        FileType = file.ContentType,
                        FileSize = file.Length,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _assignmentRepository.AddAttachmentAsync(attachment);
                }
            }

            return assignment.AssignmentId;
        }

        public async Task UpdateAssignmentAsync(Guid id, UpdateAssignmentDto request)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(id);
            if (assignment == null)
                throw new Exception($"Assignment with ID {id} not found.");

            assignment.Title = request.Title;
            assignment.Description = request.Description;
            assignment.DueDate = request.DueDate;
            assignment.GradeCategoryId = request.GradeCategoryId;
            assignment.AllowLateSubmission = request.AllowLateSubmission;
            assignment.UpdatedAt = DateTime.UtcNow;

            await _assignmentRepository.UpdateAsync(assignment);

            // Xóa attachments cũ nếu có yêu cầu
            if (request.RemoveAttachmentIds != null && request.RemoveAttachmentIds.Count > 0)
            {
                foreach (var attachmentId in request.RemoveAttachmentIds)
                {
                    var attachment = await _assignmentRepository.GetAttachmentByIdAsync(attachmentId);
                    if (attachment != null)
                    {
                        await _storageService.DeleteFileByUrlAsync(attachment.FileUrl);
                        await _assignmentRepository.RemoveAttachmentAsync(attachment);
                    }
                }
            }

            // Upload attachments mới nếu có
            if (request.NewAttachments != null && request.NewAttachments.Count > 0)
            {
                foreach (var file in request.NewAttachments)
                {
                    ValidateFile(file.FileName, file.Length, file.ContentType);

                    var fileUrl = await _storageService.UploadFileAsync(file, $"assignments/{id}");

                    var attachment = new AssignmentAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        AssignmentId = id,
                        FileName = file.FileName,
                        FileUrl = fileUrl,
                        FileType = file.ContentType,
                        FileSize = file.Length,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _assignmentRepository.AddAttachmentAsync(attachment);
                }
            }
        }

        public async Task DeleteAssignmentAsync(Guid id)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(id);
            if (assignment == null) throw new Exception("Assignment not found.");

            assignment.IsDeleted = true;
            assignment.UpdatedAt = DateTime.UtcNow;

            await _assignmentRepository.UpdateAsync(assignment);
        }

        public async Task<AssignmentDetailDto> GetAssignmentDetailAsync(Guid assignmentId)
        {
            var assignment = await _assignmentRepository.GetByIdWithDetailsAsync(assignmentId);
            if (assignment == null)
                throw new Exception("Assignment not found or has been deleted.");

            return new AssignmentDetailDto
            {
                AssignmentId = assignment.AssignmentId,
                ClassId = assignment.ClassId,
                AuthorName = assignment.Author?.FullName ?? "Unknown",
                GradeCategoryId = assignment.GradeCategoryId,
                GradeCategoryName = assignment.GradeCategory?.Name ?? "Unknown",
                Title = assignment.Title,
                Description = assignment.Description,
                DueDate = assignment.DueDate,
                Status = GetAssignmentStatus(assignment),
                AllowLateSubmission = assignment.AllowLateSubmission,
                CreatedAt = assignment.CreatedAt,
                UpdatedAt = assignment.UpdatedAt,
                Attachments = assignment.AssignmentAttachments.Select(a => new AttachmentDto
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                    CreatedAt = a.CreatedAt
                }).ToList()
            };
        }

        public async Task<IEnumerable<AssignmentSummaryDto>> GetAssignmentsByClassIdAsync(Guid classId)
        {
            var assignments = await _assignmentRepository.GetByClassIdAsync(classId);

            return assignments.Select(a => new AssignmentSummaryDto
            {
                AssignmentId = a.AssignmentId,
                Title = a.Title,
                DueDate = a.DueDate,
                Status = a.Status
            });
        }

        public async Task<AssignmentSubmissionsDto> GetAssignmentSubmissionsAsync(Guid assignmentId)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null) throw new Exception("Assignment not found.");

            var submissions = await _submissionRepository.GetSubmissionsByAssignmentIdAsync(assignmentId);

            return new AssignmentSubmissionsDto
            {
                AssignmentId = assignment.AssignmentId,
                Title = assignment.Title,
                DueDate = assignment.DueDate,
                Submissions = submissions.Select(s => new SubmissionBasicDto
                {
                    SubmissionId = s.SubmissionId,
                    StudentId = s.StudentId,
                    SubmittedAt = (DateTime)s.SubmittedAt,
                    Status = s.Status,
                    Grade = s.Grade
                }).ToList()
            };
        }

        private string GetAssignmentStatus(Assignment assignment)
        {
            if (assignment.IsDeleted == true) return "Deleted";
            if (assignment.DueDate < DateTime.UtcNow) return "Overdue";
            return "Published";
        }

        //private void ValidateFile(string fileName, long fileSize, string contentType)
        //{
        //    if (fileSize > MaxFileSize)
        //        throw new Exception($"File '{fileName}' exceeds maximum size of 10MB.");


        }

        //private void ValidateFile(string fileName, long fileSize, string contentType)
        //{
        //    if (fileSize > MaxFileSize)
        //        throw new Exception($"File '{fileName}' exceeds maximum size of 10MB.");


        //    if (contentType.StartsWith("image/")) return;

        //    if (!AllowedMimeTypes.Contains(contentType))
        //        throw new Exception($"File type '{contentType}' is not allowed.");
        //}
        private void ValidateFile(string fileName, long fileSize, string contentType)
        {
            if (fileSize > MaxFileSize)
                throw new Exception($"File '{fileName}' exceeds maximum size of 10MB.");

            var ext = Path.GetExtension(fileName).ToLower();

            var allowedExtensions = new[]
            {
                ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".bmp",
                ".pdf",
                ".doc", ".docx",
                ".xls", ".xlsx",
                ".ppt", ".pptx",
                ".zip", ".rar"
            };

            if (!allowedExtensions.Contains(ext))
                throw new Exception($"File '{ext}' is not allowed.");
        }

        private async Task RequireTeacherAccessAsync(Guid classId)
        {
            var classroom = await _classRepository.GetByIdAsync(classId);
            if (classroom == null) throw new Exception("Class not found.");
            if (classroom.TeacherId != _currentUserService.UserId) throw new Exception("You do not have access to this operation.");
        }

        public async Task GradeSubmissionAsync(Guid classId, Guid submissionId, GradeSubmissionDto request)
        {
            await RequireTeacherAccessAsync(classId);

            var submission = await _submissionRepository.GetByIdAsync(submissionId);
            if (submission == null) throw new Exception("Submission not found.");

            submission.Grade = request.Grade;
            submission.Status = "Graded";
            await _submissionRepository.UpdateAsync(submission);
        }

        public async Task GiveFeedbackAsync(Guid classId, Guid submissionId, FeedbackSubmissionDto request)
        {
            await RequireTeacherAccessAsync(classId);

            var submission = await _submissionRepository.GetByIdAsync(submissionId);
            if (submission == null) throw new Exception("Submission not found.");

            var feedback = new SubmissionFeedback
            {
                FeedbackId = Guid.NewGuid(),
                SubmissionId = submission.SubmissionId,
                AuthorId = _currentUserService.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Content = request.Content
            };

            await _submissionRepository.AddFeedbackAsync(feedback);
        }

        public async Task<Guid> OfflineGradeAsync(Guid classId, Guid assignmentId, OfflineGradeDto request)
        {
            await RequireTeacherAccessAsync(classId);

            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null || assignment.ClassId != classId) throw new Exception("Assignment not found in this class.");

            var existingSubmission = await _submissionRepository.GetSubmissionWithAttachmentsAsync(assignmentId, request.StudentId);
            if (existingSubmission != null)
            {
                existingSubmission.Grade = request.Grade;
                existingSubmission.Status = "Graded";
                await _submissionRepository.UpdateAsync(existingSubmission);
                return existingSubmission.SubmissionId;
            }

            // Create new for offline
            var newSubmission = new Submission
            {
                SubmissionId = Guid.NewGuid(),
                AssignmentId = assignmentId,
                StudentId = request.StudentId,
                Grade = request.Grade,
                Status = "Graded",
                SubmittedAt = DateTime.UtcNow
            };
            await _submissionRepository.AddAsync(newSubmission);
            return newSubmission.SubmissionId;
        }
    }
}
