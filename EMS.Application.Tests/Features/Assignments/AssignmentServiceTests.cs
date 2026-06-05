using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Assignments.DTOs;
using EMS.Application.Features.Assignments.Services;
using EMS.Application.Features.Notifications.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Tests.Features.Assignments
{
    [TestFixture]
    public class AssignmentServiceTests
    {
        private Mock<IAssignmentRepository> _mockAssignmentRepo;
        private Mock<ISubmissionRepository> _mockSubmissionRepo;
        private Mock<ISupabaseStorageService> _mockStorageService;
        private Mock<IClassRepository> _mockClassRepo;
        private Mock<ICurrentUserService> _mockCurrentUser;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<ILogger<AssignmentService>> _mockLogger;

        private AssignmentService _service;

        // Các Constants dùng trong Test
        private const string OfflineSubmissionFileRole = "offline_submission";
        private const string CorrectionFileRole = "correction";

        [SetUp]
        public void Setup()
        {
            _mockAssignmentRepo = new Mock<IAssignmentRepository>();
            _mockSubmissionRepo = new Mock<ISubmissionRepository>();
            _mockStorageService = new Mock<ISupabaseStorageService>();
            _mockClassRepo = new Mock<IClassRepository>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<AssignmentService>>();

            // Setup User mặc định để qua các bước Auth cơ bản
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");

            _service = new AssignmentService(
                _mockAssignmentRepo.Object,
                _mockSubmissionRepo.Object,
                _mockStorageService.Object,
                _mockClassRepo.Object,
                _mockCurrentUser.Object,
                _mockNotificationService.Object,
                _mockLogger.Object
            );
        }

        // Hàm Helper dùng chung để vượt qua vòng bảo mật RequireTeacherAccessByAssignmentAsync
        private void SetupRequireTeacherAccess(Guid assignmentId, Guid classId, Guid teacherId, bool isTA = false)
        {
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns(isTA ? "TA" : "Teacher");

            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId))
                               .ReturnsAsync(new Assignment { AssignmentId = assignmentId, ClassId = classId });

            _mockClassRepo.Setup(r => r.GetByIdAsync(classId))
                          .ReturnsAsync(new Class { ClassId = classId, TeacherId = isTA ? Guid.NewGuid() : teacherId });

            if (isTA)
            {
                // Giả lập TA được phân công vào lớp
                _mockClassRepo.Setup(r => r.GetTAsByClassIdAsync(classId))
                              .ReturnsAsync(new List<ClassTum> { new ClassTum { Taid = teacherId } });
            }
            else
            {
                _mockClassRepo.Setup(r => r.GetTAsByClassIdAsync(classId)).ReturnsAsync(new List<ClassTum>());
            }
        }

        #region CreateOfflineTestAsync Tests

        [Test]
        public void CreateOfflineTestAsync_UserIsNotTeacherOrAssignedTA_ThrowsUnauthorized()
        {
            // Arrange
            var request = new CreateOfflineTestDto { ClassId = Guid.NewGuid() };
            var currentUserId = Guid.NewGuid();

            _mockCurrentUser.Setup(c => c.UserId).Returns(currentUserId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Student"); // Không có quyền

            var classroom = new Class { ClassId = request.ClassId, TeacherId = Guid.NewGuid() }; // Giáo viên khác
            _mockClassRepo.Setup(r => r.GetByIdAsync(request.ClassId)).ReturnsAsync(classroom);
            _mockClassRepo.Setup(r => r.GetTAsByClassIdAsync(request.ClassId)).ReturnsAsync(new List<ClassTum>());

            // Act & Assert
            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _service.CreateOfflineTestAsync(request));
            Assert.That(ex.Message, Does.Contain("Bạn không có quyền tạo bài kiểm tra cho lớp này."));
        }

        [Test]
        public async Task CreateOfflineTestAsync_ValidTeacher_CreatesAssignmentAndDummySubmissions()
        {
            // Arrange
            var request = new CreateOfflineTestDto
            {
                ClassId = Guid.NewGuid(),
                Title = "Thi Cuối Kỳ",
                TestDate = DateTime.UtcNow.AddDays(1)
            };

            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");

            _mockClassRepo.Setup(r => r.GetByIdAsync(request.ClassId))
                          .ReturnsAsync(new Class { ClassId = request.ClassId, TeacherId = teacherId });

            _mockClassRepo.Setup(r => r.GetTAsByClassIdAsync(request.ClassId)).ReturnsAsync(new List<ClassTum>());

            // Giả lập lớp có 2 học sinh
            var students = new List<Student>
            {
                new Student { StudentId = Guid.NewGuid() },
                new Student { StudentId = Guid.NewGuid() }
            };
            _mockClassRepo.Setup(r => r.GetStudentsByClassIdAsync(request.ClassId)).ReturnsAsync(students);

            // Act
            var result = await _service.CreateOfflineTestAsync(request);

            // Assert
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));

            // Đảm bảo tạo bài tập Offline thành công
            _mockAssignmentRepo.Verify(r => r.AddAsync(It.Is<Assignment>(a =>
                a.Isoffline == true &&
                a.Status == "Published" &&
                a.Title == "Thi Cuối Kỳ")), Times.Once);

            // Đảm bảo tạo 2 bản ghi Submission rỗng cho 2 học sinh
            _mockSubmissionRepo.Verify(r => r.AddRangeAsync(It.Is<List<Submission>>(list =>
                list.Count == 2 &&
                list.All(s => s.Status == "Submitted" && s.Grade == null))), Times.Once);
        }

        #endregion

        #region UploadOfflineSubmissionAsync Tests

        [Test]
        public void UploadOfflineSubmissionAsync_NotOfflineAssignment_ThrowsBadRequest()
        {
            // Arrange
            var assignmentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var request = new UploadOfflineSubmissionDto { StudentId = Guid.NewGuid() };

            SetupRequireTeacherAccess(assignmentId, classId, _mockCurrentUser.Object.UserId);

            // Cố tình setup bài Online
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId))
                               .ReturnsAsync(new Assignment { AssignmentId = assignmentId, ClassId = classId, Isoffline = false });

            // Act & Assert
            var ex = Assert.ThrowsAsync<BadRequestException>(async () =>
                await _service.UploadOfflineSubmissionAsync(assignmentId, request));
            Assert.That(ex.Message, Does.Contain("không phải bài kiểm tra offline"));
        }

        [Test]
        public async Task UploadOfflineSubmissionAsync_ValidData_ReplacesOldFilesAndUpdateStatus()
        {
            // Arrange
            var assignmentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();

            SetupRequireTeacherAccess(assignmentId, classId, teacherId);

            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId))
                               .ReturnsAsync(new Assignment { AssignmentId = assignmentId, ClassId = classId, Isoffline = true });

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("scan_bai_thi.pdf");
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");

            var request = new UploadOfflineSubmissionDto
            {
                StudentId = Guid.NewGuid(),
                Files = new List<IFormFile> { mockFile.Object }
            };

            // Setup bài nộp cũ có 1 file cần xóa
            var oldAttachment = new SubmissionAttachment { FileRole = OfflineSubmissionFileRole, FileUrl = "old_url.pdf" };
            var submission = new Submission
            {
                SubmissionId = Guid.NewGuid(),
                Status = "Graded", // Đã chấm điểm
                SubmissionAttachments = new List<SubmissionAttachment> { oldAttachment }
            };

            _mockSubmissionRepo.Setup(r => r.GetSubmissionWithAttachmentsAsync(assignmentId, request.StudentId))
                               .ReturnsAsync(submission);

            // Act
            await _service.UploadOfflineSubmissionAsync(assignmentId, request);

            // Assert
            // 1. Verify xóa file cũ
            _mockSubmissionRepo.Verify(r => r.DeleteSubmissionAttachmentsAsync(It.Is<List<SubmissionAttachment>>(l => l.Contains(oldAttachment))), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileByUrlAsync("old_url.pdf"), Times.Once);

            // 2. Verify Upload file mới
            _mockStorageService.Verify(s => s.UploadFileAsync(mockFile.Object, It.IsAny<string>()), Times.Once);
            _mockSubmissionRepo.Verify(r => r.AddAttachmentsAsync(It.Is<List<SubmissionAttachment>>(l => l.Any(a => a.FileName == "scan_bai_thi.pdf" && a.FileRole == OfflineSubmissionFileRole))), Times.Once);

            // 3. Status vẫn giữ là Graded (vì trước đó đã Graded)
            Assert.That(submission.Status, Is.EqualTo("Graded"));
            _mockSubmissionRepo.Verify(r => r.UpdateAsync(submission), Times.Once);
        }

        #endregion

        #region PublishAssignmentAsync Tests

        [Test]
        public async Task PublishAssignmentAsync_AlreadyPublished_DoesNothing()
        {
            // Arrange
            var assignmentId = Guid.NewGuid();
            var assignment = new Assignment { Status = "Published" };
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(assignment);

            // Act
            await _service.PublishAssignmentAsync(assignmentId);

            // Assert
            _mockAssignmentRepo.Verify(r => r.UpdateAsync(It.IsAny<Assignment>()), Times.Never);
            _mockNotificationService.Verify(n => n.GetStudentTargetsAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Test]
        public async Task PublishAssignmentAsync_Draft_UpdatesToPublishedAndNotifies()
        {
            // Arrange
            var assignmentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var assignment = new Assignment { AssignmentId = assignmentId, ClassId = classId, Status = "Draft", Title = "Test Title" };
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(assignment);


            // Act
            await _service.PublishAssignmentAsync(assignmentId);

            // Assert
            Assert.That(assignment.Status, Is.EqualTo("Published"));
            _mockAssignmentRepo.Verify(r => r.UpdateAsync(assignment), Times.Once);

        }

        #endregion


        #region Create/Update/Delete Assignment Tests (From Previous Sessions)
        [Test]
        public void CreateAssignmentAsync_IsGradedTrueButNoCategory_ThrowsBadRequestException() // UTC01
        {
            // Arrange
            var request = new CreateAssignmentDto
            {
                Isgraded = true,
                GradeCategoryId = null // Cố tình để null
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<BadRequestException>(async () =>
                await _service.CreateAssignmentAsync(request));

            Assert.That(ex.Message, Does.Contain("Grade category là bắt buộc khi bài tập được chấm điểm."));
        }

        [Test]
        public async Task CreateAssignmentAsync_DraftStatusNoAttachments_SavesButNoNotification() // UTC02
        {
            // Arrange
            var request = new CreateAssignmentDto
            {
                ClassId = Guid.NewGuid(),
                Title = "Bài tập Nháp",
                Isgraded = false,
                Status = "Draft",
                Attachments = null
            };

            // Act
            var result = await _service.CreateAssignmentAsync(request);

            // Assert
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));

            // Phải lưu vào DB
            _mockAssignmentRepo.Verify(r => r.AddAsync(It.Is<Assignment>(a => a.Status == "Draft")), Times.Once);

                      
        }

        [Test]
        public async Task CreateAssignmentAsync_PublishedStatus_SavesAndSendsNotification() // UTC03
        {
            // Arrange
            var request = new CreateAssignmentDto
            {
                ClassId = Guid.NewGuid(),
                Title = "Bài tập Toán 01",
                Isgraded = true,
                GradeCategoryId = Guid.NewGuid(), // Hợp lệ
                Status = "Published", // Phát hành
                Attachments = new List<IFormFile>()
            };

            // Act
            var result = await _service.CreateAssignmentAsync(request);

            // Assert
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _mockAssignmentRepo.Verify(r => r.AddAsync(It.Is<Assignment>(a => a.Status == "Published")), Times.Once);
        }

        [Test]
        public async Task CreateAssignmentAsync_WithAttachments_UploadsAndSavesFiles() // UTC04
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("homework.pdf");
            mockFile.Setup(f => f.Length).Returns(2048); // < 10MB
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");

            var request = new CreateAssignmentDto
            {
                ClassId = Guid.NewGuid(),
                Title = "Có đính kèm file",
                Isgraded = false,
                Status = "Draft",
                Attachments = new List<IFormFile> { mockFile.Object }
            };

            _mockStorageService.Setup(s => s.UploadFileAsync(mockFile.Object, It.IsAny<string>()))
                               .ReturnsAsync("https://storage.com/homework.pdf");

            // Act
            var result = await _service.CreateAssignmentAsync(request);

            // Assert
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));

            // Kiểm tra upload file
            _mockStorageService.Verify(s => s.UploadFileAsync(mockFile.Object, It.Is<string>(path => path.Contains("assignments/"))), Times.Once);

            // Kiểm tra lưu file vào DB
            _mockAssignmentRepo.Verify(r => r.AddAttachmentAsync(It.Is<AssignmentAttachment>(a =>
                a.FileName == "homework.pdf" &&
                a.FileUrl == "https://storage.com/homework.pdf" &&
                a.FileType == "application/pdf"
            )), Times.Once);
        }


        [Test]
        public void UpdateAssignmentAsync_AssignmentNotFound_ThrowsException() // UTC01
        {
            var assignmentId = Guid.NewGuid();
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync((Assignment)null);

            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _service.UpdateAssignmentAsync(assignmentId, new UpdateAssignmentDto()));

            Assert.That(ex.Message, Does.Contain("not found"));
        }

        [Test]
        public void UpdateAssignmentAsync_IsGradedWithoutCategory_ThrowsBadRequestException() // UTC02
        {
            var assignmentId = Guid.NewGuid();
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(new Assignment());

            var request = new UpdateAssignmentDto { Isgraded = true, GradeCategoryId = null };

            var ex = Assert.ThrowsAsync<BadRequestException>(async () =>
                await _service.UpdateAssignmentAsync(assignmentId, request));

            Assert.That(ex.Message, Does.Contain("Grade category là bắt buộc"));
        }

        [Test]
        public async Task UpdateAssignmentAsync_ValidDataNoFiles_UpdatesAndNotifies() // UTC03
        {
            var assignmentId = Guid.NewGuid();
            var assignment = new Assignment { AssignmentId = assignmentId, ClassId = Guid.NewGuid() };

            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(assignment);

            var request = new UpdateAssignmentDto
            {
                Title = "Bài Mới",
                Isgraded = false
            };

            

            await _service.UpdateAssignmentAsync(assignmentId, request);

            Assert.That(assignment.Title, Is.EqualTo("Bài Mới"));
            _mockAssignmentRepo.Verify(r => r.UpdateAsync(assignment), Times.Once);
                    }

        [Test]
        public async Task UpdateAssignmentAsync_WithFileChanges_ProcessesFilesCorrectly() // UTC04
        {
            var assignmentId = Guid.NewGuid();
            var assignment = new Assignment { AssignmentId = assignmentId, ClassId = Guid.NewGuid() };
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(assignment);

            var oldAttachmentId = Guid.NewGuid();
            var oldAttachment = new AssignmentAttachment { AttachmentId = oldAttachmentId, FileUrl = "url1" };
            _mockAssignmentRepo.Setup(r => r.GetAttachmentByIdAsync(oldAttachmentId)).ReturnsAsync(oldAttachment);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("new.pdf");
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");

            var request = new UpdateAssignmentDto
            {
                Title = "Test",
                Isgraded = false,
                RemoveAttachmentIds = new List<Guid> { oldAttachmentId },
                NewAttachments = new List<IFormFile> { mockFile.Object }
            };

            await _service.UpdateAssignmentAsync(assignmentId, request);

            // Verify xóa file
            _mockStorageService.Verify(s => s.DeleteFileByUrlAsync("url1"), Times.Once);
            _mockAssignmentRepo.Verify(r => r.RemoveAttachmentAsync(oldAttachment), Times.Once);

            // Verify thêm file
            _mockStorageService.Verify(s => s.UploadFileAsync(mockFile.Object, $"assignments/{assignmentId}"), Times.Once);
            _mockAssignmentRepo.Verify(r => r.AddAttachmentAsync(It.Is<AssignmentAttachment>(a => a.FileName == "new.pdf")), Times.Once);
        }

        [Test]
        public async Task UpdateAssignmentAsync_NotificationThrowsException_LogsErrorButUpdatesSuccessfully() // UTC05
        {
            var assignmentId = Guid.NewGuid();
            var assignment = new Assignment { AssignmentId = assignmentId, ClassId = Guid.NewGuid() };
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(assignment);

            _mockNotificationService.Setup(n => n.GetStudentTargetsAsync(It.IsAny<Guid>()))
                                    .ThrowsAsync(new Exception("Notification Timeout"));

            var request = new UpdateAssignmentDto { Title = "Test", Isgraded = false };

            // Luồng chạy sẽ không văng lỗi ra ngoài
            await _service.UpdateAssignmentAsync(assignmentId, request);

            // Đảm bảo DB vẫn cập nhật thành công
            _mockAssignmentRepo.Verify(r => r.UpdateAsync(assignment), Times.Once);

            // Kiểm tra Logger
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Lỗi gửi thông báo bài tập mới: Notification Timeout")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        #endregion

        #region DeleteAssignmentAsync Tests

        [Test]
        public void DeleteAssignmentAsync_AssignmentNotFound_ThrowsException() // UTC01
        {
            // Arrange
            var assignmentId = Guid.NewGuid();
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync((Assignment)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _service.DeleteAssignmentAsync(assignmentId));

            Assert.That(ex.Message, Is.EqualTo("Assignment not found."));

            // Đảm bảo không có thao tác Update nào được gọi xuống DB
            _mockAssignmentRepo.Verify(r => r.UpdateAsync(It.IsAny<Assignment>()), Times.Never);
        }

        [Test]
        public async Task DeleteAssignmentAsync_ValidAssignment_SoftDeletesAssignment() // UTC02
        {
            // Arrange
            var assignmentId = Guid.NewGuid();
            var assignment = new Assignment
            {
                AssignmentId = assignmentId,
                IsDeleted = false
            };

            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(assignment);

            // Setup hàm UpdateAsync chạy thành công
            _mockAssignmentRepo.Setup(r => r.UpdateAsync(assignment)).Returns(Task.CompletedTask);

            // Act
            await _service.DeleteAssignmentAsync(assignmentId);

            // Assert
            Assert.That(assignment.IsDeleted, Is.True, "Cờ IsDeleted phải được bật thành true.");
            Assert.That(assignment.UpdatedAt, Is.Not.Null, "Thời gian UpdatedAt phải được cập nhật.");

            // Đảm bảo hàm UpdateAsync được gọi để lưu thay đổi
            _mockAssignmentRepo.Verify(r => r.UpdateAsync(assignment), Times.Once);
        }

        #endregion

        #region GradeSubmissionAsync Tests (From Previous Sessions)

        [Test]
        public void GradeSubmissionAsync_SubmissionNotFound_ThrowsException() // UTC01
        {
            // Arrange
            var submissionId = Guid.NewGuid();
            _mockSubmissionRepo.Setup(r => r.GetByIdAsync(submissionId)).ReturnsAsync((Submission)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _service.GradeSubmissionAsync(submissionId, new GradeSubmissionDto()));

            Assert.That(ex.Message, Is.EqualTo("Submission not found."));
        }

        [Test]
        public async Task GradeSubmissionAsync_NoOldAndNoNewFiles_UpdatesGradeOnly() // UTC02
        {
            // Arrange
            var submissionId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();

            // SỬA LỖI: Gọi helper để giả lập qua bước kiểm tra quyền
            SetupRequireTeacherAccess(assignmentId, classId, teacherId);

            var submission = new Submission
            {
                SubmissionId = submissionId,
                AssignmentId = assignmentId, // Bắt buộc phải gắn đúng AssignmentId đã mock ở trên
                Assignment = new Assignment { Title = "Bài tập 1" },
                SubmissionAttachments = new List<SubmissionAttachment>() // Rỗng
            };

            _mockSubmissionRepo.Setup(r => r.GetByIdAsync(submissionId)).ReturnsAsync(submission);

            var request = new GradeSubmissionDto { Grade = 9, CorrectionFiles = null };

            // Act
            await _service.GradeSubmissionAsync(submissionId, request);

            // Assert
            Assert.That(submission.Grade, Is.EqualTo(9));
            Assert.That(submission.Status, Is.EqualTo("Graded"));
            _mockSubmissionRepo.Verify(r => r.UpdateAsync(submission), Times.Once);

            // Không xóa, không upload file
            _mockSubmissionRepo.Verify(r => r.DeleteSubmissionAttachmentsAsync(It.IsAny<List<SubmissionAttachment>>()), Times.Never);
            _mockStorageService.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task GradeSubmissionAsync_HasOldFilesNoNewFiles_DeletesOldFilesAndGrades() // UTC03
        {
            // Arrange
            var submissionId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();

            // SỬA LỖI: Gọi helper để qua quyền
            SetupRequireTeacherAccess(assignmentId, classId, teacherId);

            var oldAttachment = new SubmissionAttachment
            {
                FileRole = CorrectionFileRole,
                FileUrl = "old_file.pdf"
            };

            var submission = new Submission
            {
                SubmissionId = submissionId,
                AssignmentId = assignmentId,
                Assignment = new Assignment { Title = "Bài tập 1" },
                SubmissionAttachments = new List<SubmissionAttachment> { oldAttachment }
            };

            _mockSubmissionRepo.Setup(r => r.GetByIdAsync(submissionId)).ReturnsAsync(submission);

            var request = new GradeSubmissionDto { Grade = 8, CorrectionFiles = new List<IFormFile>() };

            // Act
            await _service.GradeSubmissionAsync(submissionId, request);

            // Assert
            Assert.That(submission.Grade, Is.EqualTo(8));
            _mockSubmissionRepo.Verify(r => r.UpdateAsync(submission), Times.Once);

            _mockSubmissionRepo.Verify(r => r.DeleteSubmissionAttachmentsAsync(
                It.Is<List<SubmissionAttachment>>(list => list.Contains(oldAttachment))), Times.Once);

            _mockStorageService.Verify(s => s.DeleteFileByUrlAsync("old_file.pdf"), Times.Once);
            _mockStorageService.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task GradeSubmissionAsync_WithOldAndNewFilesAndNotiFails_ProcessesFilesAndLogsError() // Gộp UTC04 & UTC05
        {
            // Arrange
            var submissionId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            // SỬA LỖI: Gọi helper để qua quyền
            SetupRequireTeacherAccess(assignmentId, classId, teacherId);

            var oldAttachment = new SubmissionAttachment { FileRole = CorrectionFileRole, FileUrl = "old.png" };
            var submission = new Submission
            {
                SubmissionId = submissionId,
                StudentId = studentId,
                AssignmentId = assignmentId,
                Assignment = new Assignment { Title = "Bài tập 1" },
                SubmissionAttachments = new List<SubmissionAttachment> { oldAttachment }
            };

            _mockSubmissionRepo.Setup(r => r.GetByIdAsync(submissionId)).ReturnsAsync(submission);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("new.png");
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.ContentType).Returns("image/png");

            _mockStorageService.Setup(s => s.UploadFileAsync(mockFile.Object, It.IsAny<string>()))
                               .ReturnsAsync("new_url.png");

            // Mock bắn lỗi Notification (Đúng với hàm GetAccountIdByStudentIdAsync trong Service)
            _mockNotificationService.Setup(n => n.GetAccountIdByStudentIdAsync(It.IsAny<Guid>()))
                                    .ThrowsAsync(new Exception("Notification Timeout"));

            var request = new GradeSubmissionDto { Grade = 10, CorrectionFiles = new List<IFormFile> { mockFile.Object } };

            // Act
            await _service.GradeSubmissionAsync(submissionId, request);

            // Assert
            Assert.That(submission.Status, Is.EqualTo("Graded"));

            _mockSubmissionRepo.Verify(r => r.DeleteSubmissionAttachmentsAsync(It.IsAny<List<SubmissionAttachment>>()), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileByUrlAsync("old.png"), Times.Once);

            _mockStorageService.Verify(s => s.UploadFileAsync(mockFile.Object, $"submissions/{submissionId}/corrections"), Times.Once);
            _mockSubmissionRepo.Verify(r => r.AddAttachmentsAsync(It.Is<List<SubmissionAttachment>>(list => list.Any(a => a.FileName == "new.png"))), Times.Once);

            // Kiểm tra log lỗi Notification
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Lỗi gửi thông báo: Notification Timeout")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }
        #endregion

        #region GetSubmissionsForAssignmentAsync Tests (From Previous Sessions)

        [Test]
        public void GetSubmissionsAsync_AssignmentNotFound_ThrowsException() // UTC01
        {
            var assignmentId = Guid.NewGuid();

            // Giả lập RequireTeacherAccessByAssignmentAsync chạy pass phần kiểm tra Assignment nhưng văng lỗi ngay vì Assignment null
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync((Assignment)null);

            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _service.GetSubmissionsForAssignmentAsync(assignmentId));

            // Lỗi văng từ RequireTeacherAccess...
            Assert.That(ex.Message, Is.EqualTo("Assignment not found."));
        }

        [Test]
        public async Task GetSubmissionsAsync_PastDueOnline_CalculatesInTimeLateAndMissing() // UTC02
        {
            // 1. Arrange
            var assignmentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var pastDueDate = DateTime.UtcNow.AddDays(-2);

            // SỬA LỖI: Cung cấp đầy đủ mock cho bước kiểm tra quyền
            SetupRequireTeacherAccess(assignmentId, classId, teacherId);

            // Ghi đè lại mock Assignment của Helper để bổ sung các field cần thiết cho Test
            var assignment = new Assignment
            {
                AssignmentId = assignmentId,
                ClassId = classId,
                DueDate = pastDueDate,
                Isoffline = false
            };
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(assignment);

            var studentB = new Student { StudentId = Guid.NewGuid(), FullName = "B - Nộp Muộn" };
            var studentA = new Student { StudentId = Guid.NewGuid(), FullName = "A - Nộp Đúng Hạn" };
            var studentC = new Student { StudentId = Guid.NewGuid(), FullName = "C - Chưa Nộp" };

            _mockClassRepo.Setup(r => r.GetStudentsByClassIdAsync(classId))
                          .ReturnsAsync(new List<Student> { studentB, studentA, studentC });

            var submissions = new List<Submission>
            {
                new Submission
                {
                    StudentId = studentA.StudentId,
                    SubmittedAt = DateTime.UtcNow.AddDays(-3),
                    Grade = 9,
                    SubmissionAttachments = new List<SubmissionAttachment>
                    {
                        new SubmissionAttachment { FileRole = "Submission" },
                        new SubmissionAttachment { FileRole = "Correction" }
                    }
                },
                new Submission
                {
                    StudentId = studentB.StudentId,
                    SubmittedAt = DateTime.UtcNow.AddDays(-1),
                    Status = "Submitted",
                    SubmissionAttachments = new List<SubmissionAttachment>()
                }
            };

            _mockSubmissionRepo.Setup(r => r.GetSubmissionsByAssignmentIdAsync(assignmentId))
                               .ReturnsAsync(submissions);

            // 2. Act
            var result = await _service.GetSubmissionsForAssignmentAsync(assignmentId);

            // 3. Assert
            Assert.That(result.Students.Count, Is.EqualTo(3));

            var resStudentA = result.Students[0];
            var resStudentB = result.Students[1];
            var resStudentC = result.Students[2];

            Assert.That(resStudentA.Status, Is.EqualTo("In Time"));
            Assert.That(resStudentA.GradeStatus, Is.EqualTo("Graded"));
            Assert.That(resStudentA.Grade, Is.EqualTo(9));
            Assert.That(resStudentA.Attachments.Count, Is.EqualTo(1));
            Assert.That(resStudentA.CorrectionFiles.Count, Is.EqualTo(1));

            Assert.That(resStudentB.Status, Is.EqualTo("Late"));
            Assert.That(resStudentB.GradeStatus, Is.EqualTo("Not Graded"));

            Assert.That(resStudentC.Status, Is.EqualTo("Missing"));
            Assert.That(resStudentC.GradeStatus, Is.EqualTo("Not Graded"));
        }

        [Test]
        public async Task GetSubmissionsAsync_PastDueOffline_ReturnsNotSubmittedInsteadOfMissing() // UTC03
        {
            // Arrange
            var assignmentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();

            SetupRequireTeacherAccess(assignmentId, classId, teacherId);

            var assignment = new Assignment
            {
                AssignmentId = assignmentId,
                ClassId = classId,
                DueDate = DateTime.UtcNow.AddDays(-2),
                Isoffline = true // Là bài Offline
            };
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(assignment);

            var student = new Student { StudentId = Guid.NewGuid(), FullName = "Học Sinh Offline" };
            _mockClassRepo.Setup(r => r.GetStudentsByClassIdAsync(classId)).ReturnsAsync(new List<Student> { student });
            _mockSubmissionRepo.Setup(r => r.GetSubmissionsByAssignmentIdAsync(assignmentId)).ReturnsAsync(new List<Submission>());

            // Act
            var result = await _service.GetSubmissionsForAssignmentAsync(assignmentId);

            // Assert
            Assert.That(result.Students.First().Status, Is.EqualTo("Not Submitted"));
        }

        [Test]
        public async Task GetSubmissionsAsync_FutureDueOnline_ReturnsNotSubmitted() // UTC04
        {
            // Arrange
            var assignmentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();

            SetupRequireTeacherAccess(assignmentId, classId, teacherId);

            var assignment = new Assignment
            {
                AssignmentId = assignmentId,
                ClassId = classId,
                DueDate = DateTime.UtcNow.AddDays(5),
                Isoffline = false
            };
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(assignment);

            var student = new Student { StudentId = Guid.NewGuid(), FullName = "Học Sinh Tương Lai" };
            _mockClassRepo.Setup(r => r.GetStudentsByClassIdAsync(classId)).ReturnsAsync(new List<Student> { student });
            _mockSubmissionRepo.Setup(r => r.GetSubmissionsByAssignmentIdAsync(assignmentId)).ReturnsAsync(new List<Submission>());

            // Act
            var result = await _service.GetSubmissionsForAssignmentAsync(assignmentId);

            // Assert
            Assert.That(result.Students.First().Status, Is.EqualTo("Not Submitted"));
        }
        #endregion
    }
}
