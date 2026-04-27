using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using EMS.Application.Common.DTOs;
using EMS.Application.Features.Assignments.DTOs;
using EMS.Application.Features.Assignments.Services;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Notifications.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;

namespace EMS.Application.Tests.Features.Assignments
{
    [TestFixture]
    public class StudentAssignmentServiceTests
    {
        private Mock<ICurrentUserService> _mockCurrentUser;
        private Mock<IAssignmentRepository> _mockAssignmentRepo;
        private Mock<ISupabaseStorageService> _mockStorageService;
        private Mock<ISubmissionRepository> _mockSubmissionRepo;
        private Mock<IClassRepository> _mockClassRepo;
        private Mock<INotificationService> _mockNotificationService;
        private StudentAssignmentService _service;

        [SetUp]
        public void Setup()
        {
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockAssignmentRepo = new Mock<IAssignmentRepository>();
            _mockStorageService = new Mock<ISupabaseStorageService>();
            _mockSubmissionRepo = new Mock<ISubmissionRepository>();
            _mockClassRepo = new Mock<IClassRepository>();
            _mockNotificationService = new Mock<INotificationService>();

            _service = new StudentAssignmentService(
                _mockCurrentUser.Object,
                _mockAssignmentRepo.Object,
                _mockStorageService.Object,
                _mockSubmissionRepo.Object,
                _mockNotificationService.Object,
                _mockClassRepo.Object
            );
        }

        // Helper tạo Mock File hợp lệ để pass qua hàm tĩnh DataValidator.ValidateFile
        private Mock<IFormFile> CreateMockFile(string fileName, long length)
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(length);
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            return mockFile;
        }

        #region 1. GetClassAssignmentsAsync Tests

        [Test]
        public void GetClassAssignmentsAsync_StudentIdNull_ThrowsUnauthorized()
        {
            _mockCurrentUser.Setup(c => c.StudentId).Returns((Guid?)null);

            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _service.GetClassAssignmentsAsync(Guid.NewGuid(), new AssignmentFilter()));
            Assert.That(ex.Message, Does.Contain("Student ID is missing"));
        }

        [Test]
        public async Task GetClassAssignmentsAsync_ValidData_CalculatesStatusCorrectly()
        {
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var now = DateTime.UtcNow;
            var assignments = new List<Assignment>
            {
                // 1. Đã chấm (Có SubmittedAt + Có Grade)
                new Assignment { AssignmentId = Guid.NewGuid(), Submissions = new List<Submission> { new Submission { SubmittedAt = now, Grade = 9 } } },
                
                // 2. Đã nộp (Có SubmittedAt + Grade null)
                new Assignment { AssignmentId = Guid.NewGuid(), Submissions = new List<Submission> { new Submission { SubmittedAt = now, Grade = null } } },
                
                // 3. Quá hạn (Chưa nộp + DueDate < now)
                new Assignment { AssignmentId = Guid.NewGuid(), DueDate = now.AddDays(-1), Submissions = new List<Submission>() },
                
                // 4. Chưa nộp (Chưa nộp + DueDate > now)
                new Assignment { AssignmentId = Guid.NewGuid(), DueDate = now.AddDays(1), Submissions = new List<Submission>() }
            };

            _mockAssignmentRepo.Setup(r => r.GetStudentAssignmentsAsync(classId, studentId, 1, 10))
                               .ReturnsAsync((assignments, 4));

            var result = await _service.GetClassAssignmentsAsync(classId, new AssignmentFilter { Page = 1, Size = 10 });

            Assert.That(result.TotalCount, Is.EqualTo(4));
            Assert.That(result.Items[0].StudentStatus, Is.EqualTo("Đã chấm"));
            Assert.That(result.Items[1].StudentStatus, Is.EqualTo("Đã nộp"));
            Assert.That(result.Items[2].StudentStatus, Is.EqualTo("Quá hạn"));
            Assert.That(result.Items[3].StudentStatus, Is.EqualTo("Chưa nộp"));
        }

        #endregion

        #region 2. GetClassAssignmentsDetailAsync Tests

        [Test]
        public void GetClassAssignmentsDetailAsync_StudentIdNull_ThrowsUnauthorized()
        {
            _mockCurrentUser.Setup(c => c.StudentId).Returns((Guid?)null);
            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _service.GetClassAssignmentsDetailAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("Đăng nhập bằng tài khoản học sinh"));
        }

        [Test]
        public void GetClassAssignmentsDetailAsync_AssignmentNotFound_ThrowsKeyNotFound()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            _mockAssignmentRepo.Setup(r => r.GetAssignmentDetailAsync(It.IsAny<Guid>(), studentId))
                               .ReturnsAsync(((Assignment)null, (Submission)null)); // Giả lập k tìm thấy

            var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.GetClassAssignmentsDetailAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("Không tìm thấy bài tập"));
        }

        [Test]
        public void GetClassAssignmentsDetailAsync_NotEnrolled_ThrowsException()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var assignment = new Assignment { ClassId = Guid.NewGuid() };
            _mockAssignmentRepo.Setup(r => r.GetAssignmentDetailAsync(It.IsAny<Guid>(), studentId))
                               .ReturnsAsync((assignment, (Submission)null));
            _mockClassRepo.Setup(r => r.IsStudentAlreadyEnrolledAsync(assignment.ClassId, studentId)).ReturnsAsync(false);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.GetClassAssignmentsDetailAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("không có quyền xem bài tập này"));
        }

        [Test]
        public async Task GetClassAssignmentsDetailAsync_Valid_MapsDataCorrectly()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var assignment = new Assignment { ClassId = Guid.NewGuid(), Title = "Bài Test" };
            var submission = new Submission
            {
                Grade = 8,
                SubmissionAttachments = new List<SubmissionAttachment>
                {
                    new SubmissionAttachment { FileRole = "submission", FileName = "bai_lam.pdf" },
                    new SubmissionAttachment { FileRole = "correction", FileName = "sua_bai.pdf" }
                }
            };

            _mockAssignmentRepo.Setup(r => r.GetAssignmentDetailAsync(It.IsAny<Guid>(), studentId))
                               .ReturnsAsync((assignment, submission));
            _mockClassRepo.Setup(r => r.IsStudentAlreadyEnrolledAsync(assignment.ClassId, studentId)).ReturnsAsync(true);

            var result = await _service.GetClassAssignmentsDetailAsync(Guid.NewGuid());

            Assert.That(result.Title, Is.EqualTo("Bài Test"));
            Assert.That(result.MySubmission, Is.Not.Null);
            Assert.That(result.MySubmission.Grade, Is.EqualTo(8));
            Assert.That(result.MySubmission.Attachments.Count, Is.EqualTo(1)); // Chỉ lấy role submission
            Assert.That(result.MySubmission.Corrections.Count, Is.EqualTo(1)); // Chỉ lấy role correction
        }

        #endregion

        #region 3. SubmitAssignmentAsync Tests

        [Test]
        public void SubmitAssignmentAsync_PastDueAndNoLateSubmission_ThrowsException()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var assignment = new Assignment { DueDate = DateTime.UtcNow.AddDays(-1), AllowLateSubmission = false };
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(assignment);
            _mockClassRepo.Setup(r => r.IsStudentAlreadyEnrolledAsync(It.IsAny<Guid>(), studentId)).ReturnsAsync(true);

            var request = new SubmitAssignmentRequest { Files = new List<IFormFile>() };

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.SubmitAssignmentAsync(Guid.NewGuid(), request));
            Assert.That(ex.Message, Does.Contain("Đã hết hạn nộp bài"));
        }

        [Test]
        public void SubmitAssignmentAsync_AlreadyGraded_ThrowsException()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var assignment = new Assignment { DueDate = DateTime.UtcNow.AddDays(1) };
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(assignment);
            _mockClassRepo.Setup(r => r.IsStudentAlreadyEnrolledAsync(It.IsAny<Guid>(), studentId)).ReturnsAsync(true);

            var existingSubmission = new Submission { Grade = 8 }; // Đã chấm điểm
            _mockSubmissionRepo.Setup(r => r.GetSubmissionWithAttachmentsAsync(It.IsAny<Guid>(), studentId)).ReturnsAsync(existingSubmission);

            var request = new SubmitAssignmentRequest { Files = new List<IFormFile>() };

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.SubmitAssignmentAsync(Guid.NewGuid(), request));
            Assert.That(ex.Message, Does.Contain("Bài tập đã được chấm"));
        }

        [Test]
        public async Task SubmitAssignmentAsync_FirstTimeSubmission_CreatesAndNotifies()
        {
            var studentId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var assignment = new Assignment { ClassId = Guid.NewGuid(), DueDate = DateTime.UtcNow.AddDays(1) };
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(assignment);
            _mockClassRepo.Setup(r => r.IsStudentAlreadyEnrolledAsync(assignment.ClassId, studentId)).ReturnsAsync(true);

            _mockSubmissionRepo.Setup(r => r.GetSubmissionWithAttachmentsAsync(assignmentId, studentId)).ReturnsAsync((Submission)null); // Nộp lần đầu

            var mockFile = CreateMockFile("test.pdf", 1024);
            _mockStorageService.Setup(s => s.UploadFileAsync(mockFile.Object, It.IsAny<string>())).ReturnsAsync("url.pdf");

            // Mock Notification Info
            var assignmentInfo = new Assignment { ClassId = assignment.ClassId, Class = new Class { TeacherId = Guid.NewGuid() } };
            _mockAssignmentRepo.Setup(r => r.GetWithClassByIdAsync(assignmentId)).ReturnsAsync(assignmentInfo);

            var request = new SubmitAssignmentRequest { Files = new List<IFormFile> { mockFile.Object } };

            var result = await _service.SubmitAssignmentAsync(assignmentId, request);

            Assert.That(result, Is.True);
            _mockStorageService.Verify(s => s.UploadFileAsync(mockFile.Object, "submissions"), Times.Once);
            _mockSubmissionRepo.Verify(r => r.AddAsync(It.Is<Submission>(s => s.SubmissionAttachments.Count == 1)), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), null, "Bài nộp mới", It.IsAny<string>(), It.IsAny<string>(), "Submission"), Times.Once);
        }

        [Test]
        public async Task SubmitAssignmentAsync_Resubmission_DeletesOldFilesAndUpdates()
        {
            var studentId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var assignment = new Assignment { DueDate = DateTime.UtcNow.AddDays(1) };
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(assignment);
            _mockClassRepo.Setup(r => r.IsStudentAlreadyEnrolledAsync(It.IsAny<Guid>(), studentId)).ReturnsAsync(true);

            var oldAttachment = new SubmissionAttachment { FileUrl = "old.pdf" };
            var existingSubmission = new Submission { Grade = null, SubmissionAttachments = new List<SubmissionAttachment> { oldAttachment } }; // Nộp lại (Chưa chấm)
            _mockSubmissionRepo.Setup(r => r.GetSubmissionWithAttachmentsAsync(assignmentId, studentId)).ReturnsAsync(existingSubmission);

            var mockFile = CreateMockFile("new.pdf", 1024);
            _mockStorageService.Setup(s => s.UploadFileAsync(mockFile.Object, It.IsAny<string>())).ReturnsAsync("new.pdf");

            var request = new SubmitAssignmentRequest { Files = new List<IFormFile> { mockFile.Object } };

            await _service.SubmitAssignmentAsync(assignmentId, request);

            _mockSubmissionRepo.Verify(r => r.DeleteSubmissionAttachmentsAsync(It.Is<List<SubmissionAttachment>>(l => l.Contains(oldAttachment))), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileByUrlAsync("old.pdf"), Times.Once);
            _mockSubmissionRepo.Verify(r => r.AddAttachmentsAsync(It.IsAny<List<SubmissionAttachment>>()), Times.Once);
            _mockSubmissionRepo.Verify(r => r.UpdateAsync(existingSubmission), Times.Once);
        }

        #endregion

        #region 4. UnsubmitAssignmentAsync Tests

        [Test]
        public void UnsubmitAssignmentAsync_SubmissionNotFound_ThrowsException()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);
            _mockSubmissionRepo.Setup(r => r.GetSubmissionWithAttachmentsAsync(It.IsAny<Guid>(), studentId)).ReturnsAsync((Submission)null);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.UnsubmitAssignmentAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("Bạn chưa nộp bài tập này"));
        }

        [Test]
        public void UnsubmitAssignmentAsync_AlreadyGraded_ThrowsException()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);
            var existingSubmission = new Submission { Grade = 9 };
            _mockSubmissionRepo.Setup(r => r.GetSubmissionWithAttachmentsAsync(It.IsAny<Guid>(), studentId)).ReturnsAsync(existingSubmission);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.UnsubmitAssignmentAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("đã được chấm điểm"));
        }

        [Test]
        public void UnsubmitAssignmentAsync_PastDueAndNoLateSubmission_ThrowsException()
        {
            var studentId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var existingSubmission = new Submission { Grade = null };
            _mockSubmissionRepo.Setup(r => r.GetSubmissionWithAttachmentsAsync(assignmentId, studentId)).ReturnsAsync(existingSubmission);

            var assignment = new Assignment { DueDate = DateTime.UtcNow.AddDays(-1), AllowLateSubmission = false };
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(assignment);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.UnsubmitAssignmentAsync(assignmentId));
            Assert.That(ex.Message, Does.Contain("Đã quá hạn nộp bài"));
        }

        [Test]
        public async Task UnsubmitAssignmentAsync_Valid_DeletesFromDbAndStorage()
        {
            var studentId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var attachment = new SubmissionAttachment { FileUrl = "url.pdf" };
            var existingSubmission = new Submission { Grade = null, SubmissionAttachments = new List<SubmissionAttachment> { attachment } };

            _mockSubmissionRepo.Setup(r => r.GetSubmissionWithAttachmentsAsync(assignmentId, studentId)).ReturnsAsync(existingSubmission);
            _mockAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(new Assignment { DueDate = DateTime.UtcNow.AddDays(1) }); // Còn hạn

            var result = await _service.UnsubmitAssignmentAsync(assignmentId);

            Assert.That(result, Is.True);
            _mockSubmissionRepo.Verify(r => r.DeleteSubmissionAttachmentsAsync(It.Is<List<SubmissionAttachment>>(l => l.Contains(attachment))), Times.Once);
            _mockSubmissionRepo.Verify(r => r.DeleteSubmissionAsync(existingSubmission), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileByUrlAsync("url.pdf"), Times.Once);
        }

        #endregion
    }
}