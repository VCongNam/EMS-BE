using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Posts.DTOs;
using EMS.Application.Features.Posts.Services;
using EMS.Application.Features.Notifications.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;

namespace EMS.Application.Tests.Features.Posts
{
    [TestFixture]
    public class PostServiceTests
    {
        private Mock<IPostRepository> _mockPostRepo;
        private Mock<ISupabaseStorageService> _mockStorageService;
        private Mock<ICurrentUserService> _mockCurrentUser;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<ILogger<PostService>> _mockLogger;
        private Mock<IClassRepository> _mockClassRepo;

        private PostService _service;

        [SetUp]
        public void Setup()
        {
            _mockPostRepo = new Mock<IPostRepository>();
            _mockStorageService = new Mock<ISupabaseStorageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<PostService>>();
            _mockClassRepo = new Mock<IClassRepository>();

            _service = new PostService(
                _mockPostRepo.Object,
                _mockStorageService.Object,
                _mockCurrentUser.Object,
                _mockNotificationService.Object,
                _mockLogger.Object,
                _mockClassRepo.Object
            );
        }

        // Helper tạo Mock File
        private Mock<IFormFile> CreateMockFile(string fileName, long length, string contentType)
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(length);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            return mockFile;
        }

        #region 1. CreatePostAsync Tests

        [Test]
        public async Task CreatePostAsync_ValidRequestNoAttachments_SavesAndNotifies()
        {
            // Arrange
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
            var request = new CreatePostDto { ClassId = Guid.NewGuid(), Title = "Hello", Content = "World" };

            // Act
            var result = await _service.CreatePostAsync(request);

            // Assert
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _mockPostRepo.Verify(r => r.AddAsync(It.Is<Post>(p => p.Title == "Hello")), Times.Once);
            _mockStorageService.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
            _mockNotificationService.Verify(n => n.GetAllClassTargetsAsync(request.ClassId), Times.Once);
        }

        [Test]
        public async Task CreatePostAsync_WithValidAttachments_UploadsAndSaves()
        {
            // Arrange
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
            var mockFile = CreateMockFile("doc.pdf", 1024, "application/pdf");

            var request = new CreatePostDto
            {
                ClassId = Guid.NewGuid(),
                Title = "Hello",
                Attachments = new List<IFormFile> { mockFile.Object }
            };

            _mockStorageService.Setup(s => s.UploadFileAsync(mockFile.Object, It.IsAny<string>()))
                               .ReturnsAsync("url/doc.pdf");

            // Act
            await _service.CreatePostAsync(request);

            // Assert
            _mockStorageService.Verify(s => s.UploadFileAsync(mockFile.Object, It.IsAny<string>()), Times.Once);
            _mockPostRepo.Verify(r => r.AddAttachmentAsync(It.Is<PostAttachment>(a => a.FileName == "doc.pdf")), Times.Once);
        }

        [Test]
        public void CreatePostAsync_FileExceedsLimit_ThrowsArgumentException()
        {
            // Arrange
            var mockFile = CreateMockFile("big.pdf", 11 * 1024 * 1024, "application/pdf"); // 11MB
            var request = new CreatePostDto { Attachments = new List<IFormFile> { mockFile.Object } };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _service.CreatePostAsync(request));
            Assert.That(ex.Message, Does.Contain("exceeds maximum size of 10MB"));
        }

        [Test]
        public void CreatePostAsync_InvalidMimeType_ThrowsArgumentException()
        {
            // Arrange
            var mockFile = CreateMockFile("script.js", 1024, "application/javascript");
            var request = new CreatePostDto { Attachments = new List<IFormFile> { mockFile.Object } };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _service.CreatePostAsync(request));
            Assert.That(ex.Message, Does.Contain("is not allowed"));
        }

        #endregion

        #region 2. UpdatePostAsync Tests

        [Test]
        public void UpdatePostAsync_PostNotFound_ThrowsNotFoundException()
        {
            var postId = Guid.NewGuid();
            _mockPostRepo.Setup(r => r.GetByIdAsync(postId)).ReturnsAsync((Post)null);

            var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.UpdatePostAsync(postId, new UpdatePostDto()));
            Assert.That(ex.Message, Does.Contain("Bài đăng không tồn tại"));
        }

        [Test]
        public void UpdatePostAsync_UserNotAuthor_ThrowsForbiddenAccessException()
        {
            var postId = Guid.NewGuid();
            var post = new Post { AuthorId = Guid.NewGuid() };
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid()); // Khác AuthorId
            _mockPostRepo.Setup(r => r.GetByIdAsync(postId)).ReturnsAsync(post);

            var ex = Assert.ThrowsAsync<ForbiddenAccessException>(async () => await _service.UpdatePostAsync(postId, new UpdatePostDto()));
            Assert.That(ex.Message, Does.Contain("không có quyền chỉnh sửa"));
        }

        [Test]
        public async Task UpdatePostAsync_ValidRequest_UpdatesAndProcessesFiles()
        {
            var postId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var attachmentIdToRemove = Guid.NewGuid();

            _mockCurrentUser.Setup(c => c.UserId).Returns(authorId);
            var post = new Post { PostId = postId, AuthorId = authorId, Title = "Old" };
            _mockPostRepo.Setup(r => r.GetByIdAsync(postId)).ReturnsAsync(post);

            var oldAttachment = new PostAttachment { AttachmentId = attachmentIdToRemove, FileUrl = "delete_me" };
            _mockPostRepo.Setup(r => r.GetAttachmentByIdAsync(attachmentIdToRemove)).ReturnsAsync(oldAttachment);

            var mockFile = CreateMockFile("new.png", 1024, "image/png");

            var request = new UpdatePostDto
            {
                Title = "New Title",
                RemoveAttachmentIds = new List<Guid> { attachmentIdToRemove },
                NewAttachments = new List<IFormFile> { mockFile.Object }
            };

            // Act
            await _service.UpdatePostAsync(postId, request);

            // Assert
            Assert.That(post.Title, Is.EqualTo("New Title"));
            _mockPostRepo.Verify(r => r.UpdateAsync(post), Times.Once);

            // File removal verified
            _mockStorageService.Verify(s => s.DeleteFileByUrlAsync("delete_me"), Times.Once);
            _mockPostRepo.Verify(r => r.RemoveAttachmentAsync(oldAttachment), Times.Once);

            // File addition verified
            _mockStorageService.Verify(s => s.UploadFileAsync(mockFile.Object, It.IsAny<string>()), Times.Once);
            _mockPostRepo.Verify(r => r.AddAttachmentAsync(It.Is<PostAttachment>(a => a.FileName == "new.png")), Times.Once);
        }

        #endregion

        #region 3. DeletePostAsync Tests

        [Test]
        public void DeletePostAsync_PostNotFound_ThrowsNotFoundException()
        {
            _mockPostRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Post)null);
            var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.DeletePostAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("Bài đăng không tồn tại"));
        }

        [Test]
        public void DeletePostAsync_UserNotAuthor_ThrowsForbiddenAccessException()
        {
            var post = new Post { AuthorId = Guid.NewGuid() };
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
            _mockPostRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(post);

            var ex = Assert.ThrowsAsync<ForbiddenAccessException>(async () => await _service.DeletePostAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("không có quyền xóa"));
        }

        [Test]
        public async Task DeletePostAsync_Valid_SoftDeletesPost()
        {
            var authorId = Guid.NewGuid();
            var post = new Post { AuthorId = authorId, IsDeleted = false };
            _mockCurrentUser.Setup(c => c.UserId).Returns(authorId);
            _mockPostRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(post);

            await _service.DeletePostAsync(Guid.NewGuid());

            Assert.That(post.IsDeleted, Is.True);
            _mockPostRepo.Verify(r => r.UpdateAsync(post), Times.Once);
        }

        #endregion

        #region 4. GetPostDetailAsync Tests

        [Test]
        public void GetPostDetailAsync_PostNotFound_ThrowsNotFoundException()
        {
            _mockPostRepo.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync((Post)null);
            var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.GetPostDetailAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("Bài đăng không tồn tại"));
        }

        [Test]
        public async Task GetPostDetailAsync_Valid_ReturnsDetailedDto()
        {
            var postId = Guid.NewGuid();
            var post = new Post
            {
                PostId = postId,
                Title = "Title",
                Content = "Content",
                Author = new Account { FullName = "Teacher A" },
                PostAttachments = new List<PostAttachment> { new PostAttachment { FileName = "a.pdf" } },
                Comments = new List<Comment> { new Comment { Content = "Nice" } }
            };
            _mockPostRepo.Setup(r => r.GetByIdWithDetailsAsync(postId)).ReturnsAsync(post);

            var result = await _service.GetPostDetailAsync(postId);

            Assert.That(result.Title, Is.EqualTo("Title"));
            Assert.That(result.AuthorName, Is.EqualTo("Teacher A"));
            Assert.That(result.Attachments.Count, Is.EqualTo(1));
            Assert.That(result.Comments.Count, Is.EqualTo(1));
        }

        #endregion

        #region 5. GetPostsByClassIdAsync Tests

        [Test]
        public async Task GetPostsByClassIdAsync_ReturnsMappedList()
        {
            var classId = Guid.NewGuid();
            var posts = new List<Post>
            {
                new Post { Title = "Post 1", PostAttachments = new List<PostAttachment>(), Comments = new List<Comment>() },
                new Post { Title = "Post 2", PostAttachments = new List<PostAttachment>(), Comments = new List<Comment>() }
            };
            _mockPostRepo.Setup(r => r.GetByClassIdAsync(classId)).ReturnsAsync(posts);

            var result = await _service.GetPostsByClassIdAsync(classId);

            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.First().Title, Is.EqualTo("Post 1"));
        }

        #endregion

        #region 6. CreateCommentAsync Tests

        [Test]
        public void CreateCommentAsync_EmptyContent_ThrowsArgumentException()
        {
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.CreateCommentAsync(Guid.NewGuid(), new CreateCommentDto { Content = "   " }));
            Assert.That(ex.Message, Does.Contain("không được để trống"));
        }

        [Test]
        public void CreateCommentAsync_PostNotFound_ThrowsNotFoundException()
        {
            _mockPostRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Post)null);
            var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
                await _service.CreateCommentAsync(Guid.NewGuid(), new CreateCommentDto { Content = "Test" }));
            Assert.That(ex.Message, Does.Contain("Không tìm thấy bài viết"));
        }

        [Test]
        public void CreateCommentAsync_StudentRoleWithoutStudentId_ThrowsNotFoundException()
        {
            _mockPostRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Post());
            _mockCurrentUser.Setup(c => c.Role).Returns("Student");
            _mockCurrentUser.Setup(c => c.StudentId).Returns((Guid?)null); // Mất context học sinh

            var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
                await _service.CreateCommentAsync(Guid.NewGuid(), new CreateCommentDto { Content = "Test" }));
            Assert.That(ex.Message, Does.Contain("Không tìm thấy học sinh đang được chọn"));
        }

        [Test]
        public async Task CreateCommentAsync_ValidStudent_AddsComment()
        {
            var postId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            _mockPostRepo.Setup(r => r.GetByIdAsync(postId)).ReturnsAsync(new Post());
            _mockCurrentUser.Setup(c => c.Role).Returns("Student");
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var result = await _service.CreateCommentAsync(postId, new CreateCommentDto { Content = "CMT" });

            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _mockPostRepo.Verify(r => r.AddCommentAsync(It.Is<Comment>(c => c.AuthorId == studentId && c.Content == "CMT")), Times.Once);
        }

        [Test]
        public async Task CreateCommentAsync_ValidTeacher_AddsComment()
        {
            var postId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _mockPostRepo.Setup(r => r.GetByIdAsync(postId)).ReturnsAsync(new Post());
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");
            _mockCurrentUser.Setup(c => c.UserId).Returns(userId);

            await _service.CreateCommentAsync(postId, new CreateCommentDto { Content = "CMT" });

            _mockPostRepo.Verify(r => r.AddCommentAsync(It.Is<Comment>(c => c.AuthorId == userId)), Times.Once);
        }

        #endregion

        #region 7. DeleteCommentAsync Tests

        [Test]
        public void DeleteCommentAsync_CommentNotFound_ThrowsNotFoundException()
        {
            _mockPostRepo.Setup(r => r.GetCommentByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Comment)null);
            var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.DeleteCommentAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("Không tìm thấy bình luận"));
        }

        [Test]
        public void DeleteCommentAsync_UserNotAuthor_ThrowsForbiddenAccessException()
        {
            var comment = new Comment { AuthorId = Guid.NewGuid() };
            _mockPostRepo.Setup(r => r.GetCommentByIdAsync(It.IsAny<Guid>())).ReturnsAsync(comment);

            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid()); // Khác Author

            var ex = Assert.ThrowsAsync<ForbiddenAccessException>(async () => await _service.DeleteCommentAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("không có quyền xóa"));
        }

        [Test]
        public async Task DeleteCommentAsync_ValidAuthor_SoftDeletesComment()
        {
            var authorId = Guid.NewGuid();
            var comment = new Comment { AuthorId = authorId, IsDeleted = false };
            _mockPostRepo.Setup(r => r.GetCommentByIdAsync(It.IsAny<Guid>())).ReturnsAsync(comment);

            _mockCurrentUser.Setup(c => c.Role).Returns("Student");
            _mockCurrentUser.Setup(c => c.StudentId).Returns(authorId);

            await _service.DeleteCommentAsync(Guid.NewGuid());

            Assert.That(comment.IsDeleted, Is.True);
            _mockPostRepo.Verify(r => r.UpdateCommentAsync(comment), Times.Once);
        }

        #endregion
    }
}