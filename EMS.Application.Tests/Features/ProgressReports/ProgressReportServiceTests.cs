using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Notifications.Services;
using EMS.Application.Features.ProgressReports.DTOs;
using EMS.Application.Features.ProgressReports.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;

namespace EMS.Application.Tests.Features.ProgressReports
{
    [TestFixture]
    public class ProgressReportServiceTests
    {
        private Mock<IProgressReportRepository> _mockReportRepo;
        private Mock<ICurrentUserService> _mockCurrentUser;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<ILogger<ProgressReportService>> _mockLogger;
        private ProgressReportService _service;

        [SetUp]
        public void Setup()
        {
            _mockReportRepo = new Mock<IProgressReportRepository>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<ProgressReportService>>();

            _service = new ProgressReportService(
                _mockReportRepo.Object,
                _mockCurrentUser.Object,
                _mockNotificationService.Object,
                _mockLogger.Object
            );

            // Mặc định mock các hàm tính toán trả về list rỗng để không bị lỗi Null
            _mockReportRepo.Setup(r => r.GetSubmissionsForCalcAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                           .ReturnsAsync(new List<Submission>());
            _mockReportRepo.Setup(r => r.GetAttendancesForCalcAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                           .ReturnsAsync(new List<Attendance>());
            _mockReportRepo.Setup(r => r.GetTotalSessionsInPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                           .ReturnsAsync(10);
        }

        private Class GetValidClass()
        {
            return new Class
            {
                ClassId = Guid.NewGuid(),
                Status = "Active",
                IsDeleted = false,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1))
            };
        }

        #region 1. CreateReportAsync Tests

        [Test]
        public void CreateReportAsync_FuturePeriod_ThrowsBadRequestException()
        {
            var request = new CreateProgressReportDto
            {
                PeriodYear = DateTime.UtcNow.Year + 1, // Tương lai
                PeriodMonth = 1
            };

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.CreateReportAsync(request));
            Assert.That(ex.Message, Does.Contain("tháng trong tương lai"));
        }

        [Test]
        public void CreateReportAsync_ClassInvalid_ThrowsException()
        {
            var request = new CreateProgressReportDto { PeriodYear = DateTime.UtcNow.Year, PeriodMonth = DateTime.UtcNow.Month, ClassId = Guid.NewGuid() };
            _mockReportRepo.Setup(r => r.GetClassByIdAsync(request.ClassId)).ReturnsAsync((Class)null); // Invalid Class

            var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.CreateReportAsync(request));
            Assert.That(ex.Message, Does.Contain("Lớp học"));
        }

        [Test]
        public void CreateReportAsync_StatusReadyContentEmpty_ThrowsBadRequestException()
        {
            var request = new CreateProgressReportDto
            {
                PeriodYear = DateTime.UtcNow.Year,
                PeriodMonth = DateTime.UtcNow.Month,
                Status = "Ready",
                Content = "   " // Rỗng
            };
            _mockReportRepo.Setup(r => r.GetClassByIdAsync(request.ClassId)).ReturnsAsync(GetValidClass());

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.CreateReportAsync(request));
            Assert.That(ex.Message, Does.Contain("Vui lòng nhập nội dung nhận xét trước khi đặt trạng thái Sẵn sàng."));
        }

        [Test]
        public void CreateReportAsync_ReportAlreadyExists_ThrowsBadRequestException()
        {
            var request = new CreateProgressReportDto { PeriodYear = DateTime.UtcNow.Year, PeriodMonth = DateTime.UtcNow.Month, Status = "Draft" };
            _mockReportRepo.Setup(r => r.GetClassByIdAsync(request.ClassId)).ReturnsAsync(GetValidClass());
            _mockReportRepo.Setup(r => r.IsReportExistAsync(request.StudentId, request.ClassId, request.PeriodMonth, request.PeriodYear)).ReturnsAsync(true);

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.CreateReportAsync(request));
            Assert.That(ex.Message, Does.Contain("đã tồn tại"));
        }

        [Test]
        public async Task CreateReportAsync_ValidData_CalculatesAndSavesSuccessfully()
        {
            var request = new CreateProgressReportDto
            {
                PeriodYear = DateTime.UtcNow.Year,
                PeriodMonth = DateTime.UtcNow.Month,
                Status = "Ready",
                Content = "Good job",
                StudentId = Guid.NewGuid(),
                ClassId = Guid.NewGuid()
            };

            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
            _mockReportRepo.Setup(r => r.GetClassByIdAsync(request.ClassId)).ReturnsAsync(GetValidClass());
            _mockReportRepo.Setup(r => r.IsReportExistAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(false);

            var result = await _service.CreateReportAsync(request);

            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _mockReportRepo.Verify(r => r.AddAsync(It.Is<ProgressReport>(p => p.Status == "Ready" && p.Content == "Good job")), Times.Once);
        }

        #endregion

        #region 2. UpdateReportAsync Tests

        [Test]
        public void UpdateReportAsync_ReportNotFound_ThrowsNotFoundException()
        {
            _mockReportRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ProgressReport)null);

            var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.UpdateReportAsync(Guid.NewGuid(), new UpdateProgressReportDto()));
            Assert.That(ex.Message, Does.Contain("Báo cáo"));
        }

        [Test]
        public void UpdateReportAsync_NotAuthor_ThrowsForbiddenAccessException()
        {
            var report = new ProgressReport { TeacherId = Guid.NewGuid() };
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid()); // Khác
            _mockReportRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(report);

            Assert.ThrowsAsync<ForbiddenAccessException>(async () => await _service.UpdateReportAsync(Guid.NewGuid(), new UpdateProgressReportDto()));
        }

        [Test]
        public void UpdateReportAsync_Published_ThrowsBadRequestException()
        {
            var userId = Guid.NewGuid();
            var report = new ProgressReport { TeacherId = userId, Status = "Published" };
            _mockCurrentUser.Setup(c => c.UserId).Returns(userId);
            _mockReportRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(report);

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.UpdateReportAsync(Guid.NewGuid(), new UpdateProgressReportDto()));
            Assert.That(ex.Message, Does.Contain("không thể chỉnh sửa"));
        }

        [Test]
        public async Task UpdateReportAsync_Valid_UpdatesAndRecalculates()
        {
            var userId = Guid.NewGuid();
            var report = new ProgressReport
            {
                TeacherId = userId,
                Status = "Draft",
                PeriodMonth = DateTime.UtcNow.Month,
                PeriodYear = DateTime.UtcNow.Year,
                ClassId = Guid.NewGuid(),
                Class = GetValidClass()
            };

            _mockCurrentUser.Setup(c => c.UserId).Returns(userId);
            _mockReportRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(report);

            await _service.UpdateReportAsync(Guid.NewGuid(), new UpdateProgressReportDto { Status = "Draft", Content = "Updated" });

            Assert.That(report.Content, Is.EqualTo("Updated"));
            _mockReportRepo.Verify(r => r.UpdateAsync(report), Times.Once);
        }

        #endregion

        #region 3. DeleteReportAsync Tests

        [Test]
        public void DeleteReportAsync_ReportNotFound_ThrowsNotFoundException()
        {
            _mockReportRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ProgressReport)null);
            Assert.ThrowsAsync<NotFoundException>(async () => await _service.DeleteReportAsync(Guid.NewGuid()));
        }

        [Test]
        public void DeleteReportAsync_Published_ThrowsBadRequestException()
        {
            var userId = Guid.NewGuid();
            var report = new ProgressReport { TeacherId = userId, Status = "Published" };
            _mockCurrentUser.Setup(c => c.UserId).Returns(userId);
            _mockReportRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(report);

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.DeleteReportAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("Không thể xóa"));
        }

        [Test]
        public async Task DeleteReportAsync_Valid_DeletesReport()
        {
            var userId = Guid.NewGuid();
            var report = new ProgressReport { TeacherId = userId, Status = "Draft" };
            _mockCurrentUser.Setup(c => c.UserId).Returns(userId);
            _mockReportRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(report);

            await _service.DeleteReportAsync(Guid.NewGuid());

            _mockReportRepo.Verify(r => r.DeleteAsync(report), Times.Once);
        }

        #endregion

        #region 4. GetClassReportDetailsAsync Tests

        [Test]
        public async Task GetClassReportDetailsAsync_MapsCorrectly_PublishedVsDraft()
        {
            var classId = Guid.NewGuid();
            var studentPublishedId = Guid.NewGuid();
            var studentDraftId = Guid.NewGuid();

            _mockReportRepo.Setup(r => r.GetClassByIdAsync(classId)).ReturnsAsync(GetValidClass());

            var enrollments = new List<ClassEnrollment>
            {
                new ClassEnrollment { StudentId = studentPublishedId },
                new ClassEnrollment { StudentId = studentDraftId }
            };
            _mockReportRepo.Setup(r => r.GetActiveStudentsInClassAsync(classId)).ReturnsAsync(enrollments);

            var existingReports = new List<ProgressReport>
            {
                new ProgressReport { StudentId = studentPublishedId, Status = "Published", Gpa = 9.5m, AttendanceRate = 100m },
                new ProgressReport { StudentId = studentDraftId, Status = "Draft", Gpa = 8.0m, AttendanceRate = 50m }
            };
            _mockReportRepo.Setup(r => r.GetReportsByClassAndPeriodAsync(classId, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(existingReports);

            // Act
            var result = (await _service.GetClassReportDetailsAsync(classId, DateTime.UtcNow.Month, DateTime.UtcNow.Year)).ToList();

            // Assert
            var publishedRes = result.First(r => r.StudentId == studentPublishedId);
            var draftRes = result.First(r => r.StudentId == studentDraftId);

            // Báo cáo Published phải giữ nguyên số liệu trong DB
            Assert.That(publishedRes.Gpa, Is.EqualTo(9.5m));
            Assert.That(publishedRes.AttendanceRate, Is.EqualTo(100m));

            // Báo cáo Draft phải tính Live, vì mock rỗng nên nó sẽ ra 0m
            Assert.That(draftRes.Gpa, Is.EqualTo(0m));
            Assert.That(draftRes.AttendanceRate, Is.EqualTo(0m));
        }

        #endregion

        #region 5. SendReportAsync Tests

        [Test]
        public async Task SendReportAsync_AlreadyPublished_ReturnsEarly()
        {
            var userId = Guid.NewGuid();
            var report = new ProgressReport { TeacherId = userId, Status = "Published" };
            _mockCurrentUser.Setup(c => c.UserId).Returns(userId);
            _mockReportRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(report);

            await _service.SendReportAsync(Guid.NewGuid());

            _mockReportRepo.Verify(r => r.UpdateAsync(It.IsAny<ProgressReport>()), Times.Never);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SendReportAsync_Valid_UpdatesStatusAndNotifies()
        {
            var userId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var report = new ProgressReport
            {
                TeacherId = userId,
                Status = "Ready",
                PeriodMonth = 1,
                PeriodYear = 2024,
                StudentId = studentId,
                Student = new Student { AccountId = Guid.NewGuid() },
                Class = new Class { ClassName = "Math" }
            };

            _mockCurrentUser.Setup(c => c.UserId).Returns(userId);
            _mockReportRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(report);

            await _service.SendReportAsync(Guid.NewGuid());

            Assert.That(report.Status, Is.EqualTo("Published"));
            _mockReportRepo.Verify(r => r.UpdateAsync(report), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), studentId, "Báo cáo học tập mới", It.IsAny<string>(), It.IsAny<string>(), "Report"), Times.Once);
        }

        #endregion

        #region 6. GetReportDetailAsync Tests

        [Test]
        public void GetReportDetailAsync_NotFound_ThrowsNotFoundException()
        {
            _mockReportRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ProgressReport)null);
            Assert.ThrowsAsync<NotFoundException>(async () => await _service.GetReportDetailAsync(Guid.NewGuid()));
        }

        [Test]
        public async Task GetReportDetailAsync_Valid_ReturnsDetailedDto()
        {
            var reportId = Guid.NewGuid();
            var report = new ProgressReport
            {
                ReportId = reportId,
                Status = "Ready",
                PeriodMonth = 5,
                PeriodYear = 2024,
                ClassId = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                Title = "Title"
            };
            _mockReportRepo.Setup(r => r.GetByIdAsync(reportId)).ReturnsAsync(report);

            var result = await _service.GetReportDetailAsync(reportId);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ReportId, Is.EqualTo(reportId));
            Assert.That(result.Title, Is.EqualTo("Title"));
        }

        #endregion
    }
}