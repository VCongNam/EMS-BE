using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Assignments.DTOs;
using EMS.Application.Features.Notifications.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assignments.Services
{
    public class AssignmentService : IAssignmentService
    {
        private const string SubmissionFileRole = "submission";
        private const string CorrectionFileRole = "correction";
        private const string OfflineSubmissionFileRole = "offline_submission";

        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly ISupabaseStorageService _storageService;
        private readonly IClassRepository _classRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AssignmentService> _logger;

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
            ICurrentUserService currentUserService,
            INotificationService notificationService,
            ILogger<AssignmentService> logger)
        {
            _assignmentRepository = assignmentRepository;
            _submissionRepository = submissionRepository;
            _storageService = storageService;
            _classRepository = classRepository;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<Guid> CreateAssignmentAsync(CreateAssignmentDto request)
        {
            if (request.Isgraded && (!request.GradeCategoryId.HasValue || request.GradeCategoryId.Value == Guid.Empty))
            {
                throw new BadRequestException("Grade category là bắt buộc khi bài tập được chấm điểm.");
            }

            var assignment = new Assignment
            {
                AssignmentId = Guid.NewGuid(),
                ClassId = request.ClassId,
                AuthorId = _currentUserService.UserId,
                GradeCategoryId = request.Isgraded ? request.GradeCategoryId : null,
                Title = request.Title,
                Description = request.Description,
                DueDate = request.DueDate,
                AllowLateSubmission = request.AllowLateSubmission,
                Isgraded = request.Isgraded,
                Status = request.Status == "Published" ? "Published" : "Draft",
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

            if (assignment.Status == "Published")
            {
                await SendAssignmentNotificationAsync(assignment);
            }

            return assignment.AssignmentId;
        }

        public async Task UpdateAssignmentAsync(Guid id, UpdateAssignmentDto request)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(id);
            if (assignment == null)
                throw new Exception($"Assignment with ID {id} not found.");

            if (request.Isgraded && (!request.GradeCategoryId.HasValue || request.GradeCategoryId.Value == Guid.Empty))
            {
                throw new BadRequestException("Grade category là bắt buộc khi bài tập được chấm điểm.");
            }

            assignment.Title = request.Title;
            assignment.Description = request.Description;
            assignment.DueDate = request.DueDate;
            assignment.GradeCategoryId = request.Isgraded ? request.GradeCategoryId : null;
            assignment.AllowLateSubmission = request.AllowLateSubmission;
            assignment.Isgraded = request.Isgraded;
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

            //Notification
            try
            {
                var studentAccountIds = await _notificationService.GetStudentTargetsAsync(assignment.ClassId);
                if (studentAccountIds.Any())
                {
                    await _notificationService.SendBulkNotificationWithStudentAsync(
                            targets: studentAccountIds,
                            title: "Cập nhật bài tập",
                            content: $"Giáo viên đã sửa bài tập: {request.Title}. Hạn nộp: {request.DueDate}",
                            actionUrl: $"/student/classes/{assignment.ClassId}/assignment/{assignment.AssignmentId}",
                            type: "Assignment"
                        );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi gửi thông báo bài tập mới: {ex.Message}");
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
                GradeCategoryName = assignment.GradeCategory?.Name,
                Title = assignment.Title,
                Description = assignment.Description,
                DueDate = assignment.DueDate,
                Status = GetAssignmentStatus(assignment),
                AllowLateSubmission = assignment.AllowLateSubmission,
                IsOffline = assignment.Isoffline,
                Isgraded = assignment.Isgraded,
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
                Status = a.Status,
                Isgraded = a.Isgraded
            });
        }

        public async Task PublishAssignmentAsync(Guid assignmentId)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null) throw new Exception("Assignment not found.");

            if (assignment.Status == "Published") return;

            assignment.Status = "Published";
            assignment.UpdatedAt = DateTime.UtcNow;

            await _assignmentRepository.UpdateAsync(assignment);

            await SendAssignmentNotificationAsync(assignment);
        }

        private async Task SendAssignmentNotificationAsync(Assignment assignment)
        {
            try
            {
                var studentAccountIds = await _notificationService.GetStudentTargetsAsync(assignment.ClassId);
                if (studentAccountIds.Any())
                {
                    await _notificationService.SendBulkNotificationWithStudentAsync(
                            targets: studentAccountIds,
                            title: "Bài tập mới",
                            content: $"Giáo viên đã giao bài tập mới: {assignment.Title}. Hạn nộp: {assignment.DueDate}",
                            actionUrl: $"/student/classes/{assignment.ClassId}/assignment/{assignment.AssignmentId}",
                            type: "Assignment"
                        );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi gửi thông báo khi giao bài tập: {ex.Message}");
            }
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
            if (assignment.Status == "Draft") return "Draft";
            if (assignment.DueDate < DateTime.UtcNow) return "Overdue";
            return "Published";
        }

     
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

        private async Task RequireTeacherAccessByAssignmentAsync(Guid assignmentId)
        {
            
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null) throw new Exception("Assignment not found.");

            var tas = await _classRepository.GetTAsByClassIdAsync(assignment.ClassId);
            bool isAssigned = false;
            if (_currentUserService.Role == "TA")
            {
                isAssigned = tas.Any(ta => ta.Taid == _currentUserService.UserId);
            }
            var classroom = await _classRepository.GetByIdAsync(assignment.ClassId);
            if (classroom == null) throw new Exception("Class not found.");

            // Kiểm tra quyền
            if (classroom.TeacherId != _currentUserService.UserId && !isAssigned)
                throw new UnauthorizedAccessException("You do not have access to grade this assignment.");
        }

        public async Task GradeSubmissionAsync(Guid submissionId, GradeSubmissionDto request)
        {
            var submission = await _submissionRepository.GetByIdAsync(submissionId);
            if (submission == null) throw new Exception("Submission not found.");

            await RequireTeacherAccessByAssignmentAsync(submission.AssignmentId);

            var oldCorrectionFileUrls = new List<string>();

            // Lấy correction cũ dù có file mới hay không
            var oldCorrectionAttachments = submission.SubmissionAttachments
                .Where(a => string.Equals(a.FileRole, CorrectionFileRole, StringComparison.OrdinalIgnoreCase))
                .ToList();

            oldCorrectionFileUrls = oldCorrectionAttachments
                .Select(a => a.FileUrl)
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Cast<string>()
                .ToList();

            // Xóa correction cũ dù gửi file mới hay empty
            if (oldCorrectionAttachments.Count > 0)
            {
                await _submissionRepository.DeleteSubmissionAttachmentsAsync(oldCorrectionAttachments);
            }

            // Chỉ upload mới nếu có file
            if (request.CorrectionFiles != null && request.CorrectionFiles.Count > 0)
            {
                foreach (var file in request.CorrectionFiles)
                    ValidateFile(file.FileName, file.Length, file.ContentType);

                var newCorrectionAttachments = new List<SubmissionAttachment>();
                foreach (var file in request.CorrectionFiles)
                {
                    var fileUrl = await _storageService.UploadFileAsync(file, $"submissions/{submission.SubmissionId}/corrections");
                    newCorrectionAttachments.Add(new SubmissionAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        SubmissionId = submission.SubmissionId,
                        FileUrl = fileUrl,
                        FileName = file.FileName,
                        FileType = file.ContentType,
                        FileSize = file.Length,
                        CreatedAt = DateTime.UtcNow,
                        FileRole = CorrectionFileRole
                    });
                }
                await _submissionRepository.AddAttachmentsAsync(newCorrectionAttachments);
            }
            // Nếu empty → không add gì → correction sẽ rỗng ✓

            submission.Grade = request.Grade;
            submission.Status = "Graded";
            await _submissionRepository.UpdateAsync(submission);

            // Xóa file storage sau khi DB đã update
            if (oldCorrectionFileUrls.Count > 0)
            {
                var deleteTasks = oldCorrectionFileUrls.Select(_storageService.DeleteFileByUrlAsync);
                await Task.WhenAll(deleteTasks);
            }

            // Notification
            try
            {
                var targetAccountId = await _notificationService.GetAccountIdByStudentIdAsync(submission.StudentId);
                if (targetAccountId != null)
                {
                    await _notificationService.SendNotificationAsync(
                        targetAccountId: targetAccountId.Value,
                        studentId: submission.StudentId,
                        title: "Bài tập đã được cho điểm",
                        content: $"Giáo viên đã chấm bài tập: {submission.Assignment.Title} của bạn.",
                        actionUrl: $"/student/classes/{submission.Assignment.ClassId}/assignment/{submission.Assignment.AssignmentId}",
                        type: "Assignment"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi gửi thông báo: {ex.Message}");
            }
        }

        public async Task GiveFeedbackAsync(Guid submissionId, FeedbackSubmissionDto request)
        {
            var submission = await _submissionRepository.GetByIdAsync(submissionId);
            if (submission == null) throw new Exception("Submission not found.");

            // VÁ LỖI BẢO MẬT
            await RequireTeacherAccessByAssignmentAsync(submission.AssignmentId);

            var feedback = new SubmissionFeedback
            {
                FeedbackId = Guid.NewGuid(),
                SubmissionId = submission.SubmissionId,
                AuthorId = _currentUserService.UserId,
                CreatedAt = DateTime.UtcNow,
                Content = request.Content
            };

            await _submissionRepository.AddFeedbackAsync(feedback);
        }

        public async Task<Guid> OfflineGradeAsync(Guid assignmentId, OfflineGradeDto request)
        {
            // VÁ LỖI BẢO MẬT: Không tin tưởng ClassId do frontend gửi lên nữa
            await RequireTeacherAccessByAssignmentAsync(assignmentId);

            // Xử lý tạo mới hoặc cập nhật như cũ của bạn
            var existingSubmission = await _submissionRepository.GetSubmissionWithAttachmentsAsync(assignmentId, request.StudentId);

            if (existingSubmission != null)
            {
                existingSubmission.Grade = request.Grade;
                existingSubmission.Status = "Graded";
                await _submissionRepository.UpdateAsync(existingSubmission);
                return existingSubmission.SubmissionId;
            }

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

        public async Task<AssignmentSubmissionsListDto> GetSubmissionsForAssignmentAsync(Guid assignmentId)
        {
            await RequireTeacherAccessByAssignmentAsync(assignmentId);
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId)
                ?? throw new KeyNotFoundException("Assignment not found.");

            var studentsInClass = await _classRepository.GetStudentsByClassIdAsync(assignment.ClassId);

            var submissions = await _submissionRepository.GetSubmissionsByAssignmentIdAsync(assignmentId);

            var response = new AssignmentSubmissionsListDto
            {
                AssignmentId = assignment.AssignmentId
            };

            var currentTime = DateTime.UtcNow;

            foreach (var student in studentsInClass)
            {
                var sub = submissions.FirstOrDefault(s => s.StudentId == student.StudentId);

                var studentDto = new StudentSubmissionDto
                {
                    StudentId = student.StudentId,
                    FullName = student.FullName,
                    
                };

                if (sub != null)
                {
                    // NẾU ĐÃ NỘP BÀI
                    studentDto.SubmissionId = sub.SubmissionId;
                    studentDto.SubmittedAt = sub.SubmittedAt;
                    studentDto.Grade = sub.Grade;
                    studentDto.Attachments = sub.SubmissionAttachments
                       .Where(a => string.Equals(a.FileRole, SubmissionFileRole, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(a.FileRole, OfflineSubmissionFileRole, StringComparison.OrdinalIgnoreCase))
                        .Select(MapSubmissionFile)
                        .ToList();
                    studentDto.CorrectionFiles = sub.SubmissionAttachments
                        .Where(a => string.Equals(a.FileRole, CorrectionFileRole, StringComparison.OrdinalIgnoreCase))
                        .Select(MapSubmissionFile)
                        .ToList();
                    studentDto.GradeStatus = sub.Grade.HasValue || string.Equals(sub.Status, "Graded", StringComparison.OrdinalIgnoreCase)
                        ? "Graded"
                        : "Not Graded";

                    // Xử lý status thông minh
                   if (sub.SubmittedAt > assignment.DueDate)
                    {
                        studentDto.Status = "Late"; // Nộp muộn
                    }
                    else
                    {
                        studentDto.Status = "In Time"; // Đã nộp đúng hạn
                    }
                }
                else
                {
                    studentDto.GradeStatus = "Not Graded";

                    if (currentTime > assignment.DueDate && assignment.Isoffline != true)
                    {
                        studentDto.Status = "Missing";
                    }
                    else
                    {
                        studentDto.Status = "Not Submitted"; 
                    }
                }

                response.Students.Add(studentDto);
            }

            response.Students = response.Students.OrderBy(s => s.FullName).ToList();

            return response;
        }

        private static SubmissionFileDto MapSubmissionFile(SubmissionAttachment attachment)
        {
            return new SubmissionFileDto
            {
                AttachmentId = attachment.AttachmentId,
                FileName = attachment.FileName,
                FileUrl = attachment.FileUrl,
                FileType = attachment.FileType,
                FileSize = attachment.FileSize,
                CreatedAt = attachment.CreatedAt
            };
        }

        public async Task<StudentSubmissionDetailDto> GetStudentSubmissionDetailAsync(Guid assignmentId, Guid studentId)
        {
            await RequireTeacherAccessByAssignmentAsync(assignmentId);

            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId)
                ?? throw new KeyNotFoundException("Assignment not found.");

            var submission = await _submissionRepository.GetSubmissionDetailForTeacherAsync(assignmentId, studentId);
            if (submission == null)
                throw new KeyNotFoundException("Học sinh này chưa nộp bài tập đó.");

            return new StudentSubmissionDetailDto
            {
                SubmissionId = submission.SubmissionId,
                AssignmentId = assignmentId,
                AssignmentTitle = assignment.Title,
                StudentId = submission.StudentId,
                StudentFullName = submission.Student?.FullName ?? "Unknown",
                SubmittedAt = submission.SubmittedAt,
                Status = submission.Status ?? string.Empty,
                Grade = submission.Grade,
                Attachments = submission.SubmissionAttachments
                    .Where(a => string.Equals(a.FileRole, SubmissionFileRole, StringComparison.OrdinalIgnoreCase))
                    .Select(a => new SubmissionAttachmentDto
                    {
                        AttachmentId = a.AttachmentId,
                        FileName = a.FileName,
                        FileUrl = a.FileUrl,
                        FileType = a.FileType,
                        FileSize = a.FileSize,
                        CreatedAt = a.CreatedAt,
                        FileRole = a.FileRole
                    }).ToList(),
                CorrectionFiles = submission.SubmissionAttachments
                    .Where(a => string.Equals(a.FileRole, CorrectionFileRole, StringComparison.OrdinalIgnoreCase))
                    .Select(a => new SubmissionAttachmentDto
                    {
                        AttachmentId = a.AttachmentId,
                        FileName = a.FileName,
                        FileUrl = a.FileUrl,
                        FileType = a.FileType,
                        FileSize = a.FileSize,
                        CreatedAt = a.CreatedAt,
                        FileRole = a.FileRole
                    }).ToList(),
                Feedbacks = submission.SubmissionFeedbacks
                    .OrderBy(f => f.CreatedAt)
                    .Select(f => new SubmissionFeedbackDto
                    {
                        FeedbackId = f.FeedbackId,
                        AuthorName = f.Author?.FullName ?? "Unknown",
                        Content = f.Content,
                        CreatedAt = f.CreatedAt
                    }).ToList()
            };
        }
       

        public async Task<Guid> CreateOfflineTestAsync(CreateOfflineTestDto request)
        {
            var classroom = await _classRepository.GetByIdAsync(request.ClassId)
                ?? throw new KeyNotFoundException("Class not found.");

            var tas = await _classRepository.GetTAsByClassIdAsync(request.ClassId);
            bool isAssigned = _currentUserService.Role == "TA" &&
                              tas.Any(ta => ta.Taid == _currentUserService.UserId);

            if (classroom.TeacherId != _currentUserService.UserId && !isAssigned)
                throw new UnauthorizedAccessException("Bạn không có quyền tạo bài kiểm tra cho lớp này.");

            var assignment = new Assignment
            {
                AssignmentId = Guid.NewGuid(),
                ClassId = request.ClassId,
                AuthorId = _currentUserService.UserId,
                GradeCategoryId = request.GradeCategoryId,
                Title = request.Title,
                Description = request.Description,
                DueDate = request.TestDate,         
                Isoffline = true,
                Isgraded = true,
                AllowLateSubmission = false,
                Status = "Published",
                IsDeleted = false,
                CreatedAt = request.TestDate       
            };

            await _assignmentRepository.AddAsync(assignment);

            if (request.Attachments != null && request.Attachments.Count > 0)
            {
                foreach (var file in request.Attachments)
                {
                    ValidateFile(file.FileName, file.Length, file.ContentType);
                    var fileUrl = await _storageService.UploadFileAsync(file, $"assignments/{assignment.AssignmentId}");
                    await _assignmentRepository.AddAttachmentAsync(new AssignmentAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        AssignmentId = assignment.AssignmentId,
                        FileName = file.FileName,
                        FileUrl = fileUrl,
                        FileType = file.ContentType,
                        FileSize = file.Length,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            var students = await _classRepository.GetStudentsByClassIdAsync(request.ClassId);
            var submissions = students.Select(s => new Submission
            {
                SubmissionId = Guid.NewGuid(),
                AssignmentId = assignment.AssignmentId,
                StudentId = s.StudentId,
                Status = "Submitted",
                SubmittedAt = null,
                Grade = null
            }).ToList();

            if (submissions.Count > 0)
                await _submissionRepository.AddRangeAsync(submissions);

            return assignment.AssignmentId;
        }
        public async Task UploadOfflineSubmissionAsync(Guid assignmentId, UploadOfflineSubmissionDto request)
        {
            await RequireTeacherAccessByAssignmentAsync(assignmentId);

            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId)
                ?? throw new KeyNotFoundException("Assignment not found.");

            if (assignment.Isoffline != true)
                throw new BadRequestException("Bài tập này không phải bài kiểm tra offline.");

            if (request.Files == null || request.Files.Count == 0)
                throw new BadRequestException("Vui lòng đính kèm ít nhất 1 file.");

            var submission = await _submissionRepository.GetSubmissionWithAttachmentsAsync(assignmentId, request.StudentId)
                ?? throw new KeyNotFoundException("Không tìm thấy submission của học sinh này.");

            var oldAttachments = submission.SubmissionAttachments
                .Where(a => string.Equals(a.FileRole, OfflineSubmissionFileRole, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var oldUrls = oldAttachments
                .Select(a => a.FileUrl)
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Cast<string>()
                .ToList();

            if (oldAttachments.Count > 0)
                await _submissionRepository.DeleteSubmissionAttachmentsAsync(oldAttachments);

            var newAttachments = new List<SubmissionAttachment>();
            foreach (var file in request.Files)
            {
                ValidateFile(file.FileName, file.Length, file.ContentType);
                var fileUrl = await _storageService.UploadFileAsync(
                    file, $"submissions/{submission.SubmissionId}/offline");

                newAttachments.Add(new SubmissionAttachment
                {
                    AttachmentId = Guid.NewGuid(),
                    SubmissionId = submission.SubmissionId,
                    FileUrl = fileUrl,
                    FileName = file.FileName,
                    FileType = file.ContentType,
                    FileSize = file.Length,
                    CreatedAt = DateTime.UtcNow,
                    FileRole = OfflineSubmissionFileRole
                });
            }

            await _submissionRepository.AddAttachmentsAsync(newAttachments);

            submission.SubmittedAt = DateTime.UtcNow;
            submission.Status = string.Equals(submission.Status, "Graded", StringComparison.OrdinalIgnoreCase)
                ? "Graded"
                : "Submitted";
            await _submissionRepository.UpdateAsync(submission);

            if (oldUrls.Count > 0)
                await Task.WhenAll(oldUrls.Select(_storageService.DeleteFileByUrlAsync));
        }
        public async Task<bool> HasStudentSubmittedAsync(Guid assignmentId, Guid studentId)
        {
            // Xác minh assignment tồn tại
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null) return false;

            return await _submissionRepository.HasStudentSubmittedAsync(assignmentId, studentId);
        }
    }
}
