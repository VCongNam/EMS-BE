using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.LearningMaterials.DTOs;
using EMS.Application.Features.LearningMaterials.Services;
using EMS.Application.Features.Notifications.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;

namespace EMS.Application.Tests.Features.LearningMaterials
{
    [TestFixture]
    public class LearningMaterialServiceTests
    {
        private Mock<ILearningMaterialRepository> _mockMaterialRepo;
        private Mock<ISupabaseStorageService> _mockStorageService;
        private Mock<ICurrentUserService> _mockCurrentUser;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<ILogger<LearningMaterialService>> _mockLogger;
        private LearningMaterialService _service;

        [SetUp]
        public void Setup()
        {
            _mockMaterialRepo = new Mock<ILearningMaterialRepository>();
            _mockStorageService = new Mock<ISupabaseStorageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<LearningMaterialService>>();

            // Setup user mặc định đang đăng nhập
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

            _service = new LearningMaterialService(
                _mockMaterialRepo.Object,
                _mockStorageService.Object,
                _mockCurrentUser.Object,
                _mockNotificationService.Object,
                _mockLogger.Object
            );
        }

        // Helper mock file
        private Mock<IFormFile> CreateMockFile(string fileName, long length, string contentType)
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(length);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            return mockFile;
        }

        #region 1. CreateLearningMaterialAsync Tests

        [Test]
        public async Task CreateLearningMaterialAsync_ValidRequestNoAttachments_SavesAndNotifies()
        {
            // Arrange
            var request = new CreateLearningMaterialDto
            {
                ClassId = Guid.NewGuid(),
                Title = "Tài liệu Toán",
                Description = "Chương 1",
                Attachments = null
            };

            // Act
            var result = await _service.CreateLearningMaterialAsync(request);

            // Assert
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));

            // Kiểm tra lưu DB
            _mockMaterialRepo.Verify(r => r.AddAsync(It.Is<LearningMaterial>(m =>
                m.Title == "Tài liệu Toán" &&
                m.ClassId == request.ClassId)), Times.Once);

            // Đảm bảo không gọi logic upload file
            _mockStorageService.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);

            // Kiểm tra hàm lấy danh sách gửi thông báo được gọi
            _mockNotificationService.Verify(n => n.GetAllClassTargetsAsync(request.ClassId), Times.Once);
        }

        [Test]
        public async Task CreateLearningMaterialAsync_WithValidAttachments_UploadsAndSavesAttachments()
        {
            // Arrange
            var mockFile = CreateMockFile("doc.pdf", 1024, "application/pdf");
            var request = new CreateLearningMaterialDto
            {
                ClassId = Guid.NewGuid(),
                Title = "Có đính kèm",
                Attachments = new List<IFormFile> { mockFile.Object }
            };

            _mockStorageService.Setup(s => s.UploadFileAsync(mockFile.Object, It.IsAny<string>()))
                               .ReturnsAsync("https://storage.com/doc.pdf");

            // Act
            var result = await _service.CreateLearningMaterialAsync(request);

            // Assert
            _mockStorageService.Verify(s => s.UploadFileAsync(mockFile.Object, It.Is<string>(path => path.Contains("attachments"))), Times.Once);

            _mockMaterialRepo.Verify(r => r.AddAttachmentAsync(It.Is<MaterialAttachment>(a =>
                a.FileName == "doc.pdf" &&
                a.FileUrl == "https://storage.com/doc.pdf")), Times.Once);
        }

        [Test]
        public void CreateLearningMaterialAsync_FileSizeExceedsLimit_ThrowsException()
        {
            // Arrange
            var mockFile = CreateMockFile("big_file.pdf", 11 * 1024 * 1024, "application/pdf"); // 11MB
            var request = new CreateLearningMaterialDto
            {
                ClassId = Guid.NewGuid(),
                Attachments = new List<IFormFile> { mockFile.Object }
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.CreateLearningMaterialAsync(request));
            Assert.That(ex.Message, Does.Contain("exceeds maximum size of 10MB"));
        }

        [Test]
        public void CreateLearningMaterialAsync_InvalidFileType_ThrowsException()
        {
            // Arrange
            var mockFile = CreateMockFile("virus.exe", 1024, "application/x-msdownload");
            var request = new CreateLearningMaterialDto
            {
                ClassId = Guid.NewGuid(),
                Attachments = new List<IFormFile> { mockFile.Object }
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.CreateLearningMaterialAsync(request));
            Assert.That(ex.Message, Does.Contain("is not allowed"));
        }

        #endregion

        #region 2. UpdateLearningMaterialAsync Tests

        [Test]
        public void UpdateLearningMaterialAsync_MaterialNotFound_ThrowsException()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            _mockMaterialRepo.Setup(r => r.GetByIdAsync(materialId)).ReturnsAsync((LearningMaterial)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _service.UpdateLearningMaterialAsync(materialId, new UpdateLearningMaterialDto()));
            Assert.That(ex.Message, Does.Contain("not found"));
        }

        [Test]
        public async Task UpdateLearningMaterialAsync_ValidRequest_UpdatesBasicFields()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            var material = new LearningMaterial { MaterialId = materialId, Title = "Old Title" };

            _mockMaterialRepo.Setup(r => r.GetByIdAsync(materialId)).ReturnsAsync(material);

            var request = new UpdateLearningMaterialDto
            {
                Title = "New Title",
                Description = "New Desc"
            };

            // Act
            await _service.UpdateLearningMaterialAsync(materialId, request);

            // Assert
            Assert.That(material.Title, Is.EqualTo("New Title"));
            Assert.That(material.Description, Is.EqualTo("New Desc"));
            _mockMaterialRepo.Verify(r => r.UpdateAsync(material), Times.Once);
        }

        [Test]
        public async Task UpdateLearningMaterialAsync_RemoveAttachments_DeletesFromStorageAndDb()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            var material = new LearningMaterial { MaterialId = materialId };
            _mockMaterialRepo.Setup(r => r.GetByIdAsync(materialId)).ReturnsAsync(material);

            var attachmentId = Guid.NewGuid();
            var oldAttachment = new MaterialAttachment { AttachmentId = attachmentId, FileUrl = "url_to_delete" };

            _mockMaterialRepo.Setup(r => r.GetAttachmentByIdAsync(attachmentId)).ReturnsAsync(oldAttachment);

            var request = new UpdateLearningMaterialDto
            {
                RemoveAttachmentIds = new List<Guid> { attachmentId }
            };

            // Act
            await _service.UpdateLearningMaterialAsync(materialId, request);

            // Assert
            _mockStorageService.Verify(s => s.DeleteFileByUrlAsync("url_to_delete"), Times.Once);
            _mockMaterialRepo.Verify(r => r.RemoveAttachmentAsync(oldAttachment), Times.Once);
        }

        [Test]
        public async Task UpdateLearningMaterialAsync_AddNewAttachments_UploadsAndSaves()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            var material = new LearningMaterial { MaterialId = materialId };
            _mockMaterialRepo.Setup(r => r.GetByIdAsync(materialId)).ReturnsAsync(material);

            var mockFile = CreateMockFile("new_doc.docx", 1024, "application/msword");

            var request = new UpdateLearningMaterialDto
            {
                NewAttachments = new List<IFormFile> { mockFile.Object }
            };

            _mockStorageService.Setup(s => s.UploadFileAsync(mockFile.Object, It.IsAny<string>()))
                               .ReturnsAsync("new_url");

            // Act
            await _service.UpdateLearningMaterialAsync(materialId, request);

            // Assert
            _mockStorageService.Verify(s => s.UploadFileAsync(mockFile.Object, It.IsAny<string>()), Times.Once);
            _mockMaterialRepo.Verify(r => r.AddAttachmentAsync(It.Is<MaterialAttachment>(a => a.FileName == "new_doc.docx")), Times.Once);
        }

        #endregion

        #region 3. GetLearningMaterialDetailAsync Tests

        [Test]
        public void GetLearningMaterialDetailAsync_NotFound_ThrowsException()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            _mockMaterialRepo.Setup(r => r.GetByIdWithDetailsAsync(materialId)).ReturnsAsync((LearningMaterial)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.GetLearningMaterialDetailAsync(materialId));
            Assert.That(ex.Message, Does.Contain("not found or has been deleted"));
        }

        [Test]
        public async Task GetLearningMaterialDetailAsync_ValidMaterial_ReturnsMappedDto()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var material = new LearningMaterial
            {
                MaterialId = materialId,
                ClassId = classId,
                Title = "Chương 2: Đạo hàm",
                Description = "Lý thuyết cơ bản",
                Author = new Account { FullName = "Thầy A" },
                MaterialAttachments = new List<MaterialAttachment>
                {
                    new MaterialAttachment { AttachmentId = Guid.NewGuid(), FileName = "slide.pdf", FileSize = 2048 }
                }
            };

            _mockMaterialRepo.Setup(r => r.GetByIdWithDetailsAsync(materialId)).ReturnsAsync(material);

            // Act
            var result = await _service.GetLearningMaterialDetailAsync(materialId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.MaterialId, Is.EqualTo(materialId));
            Assert.That(result.ClassId, Is.EqualTo(classId));
            Assert.That(result.Title, Is.EqualTo("Chương 2: Đạo hàm"));
            Assert.That(result.AuthorName, Is.EqualTo("Thầy A"));
            Assert.That(result.Attachments.Count, Is.EqualTo(1));
            Assert.That(result.Attachments.First().FileName, Is.EqualTo("slide.pdf"));
            Assert.That(result.Attachments.First().FileSize, Is.EqualTo(2048));
        }

        #endregion
    }
}