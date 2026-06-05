using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using EMS.Application.Features.Sessions.Services;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Sessions.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;

namespace EMS.Application.Tests.Features.Sessions
{
    [TestFixture]
    public class StudentScheduleServiceTests
    {
        private Mock<ISessionRepository> _mockSessionRepo;
        private Mock<ICurrentUserService> _mockCurrentUser;
        private StudentScheduleService _service;

        [SetUp]
        public void Setup()
        {
            _mockSessionRepo = new Mock<ISessionRepository>();
            _mockCurrentUser = new Mock<ICurrentUserService>();

            _service = new StudentScheduleService(
                _mockSessionRepo.Object,
                _mockCurrentUser.Object
            );
        }

        #region GetStudentSchedulesAsync Tests

        [Test]
        public void GetStudentSchedulesAsync_StudentIdMissing_ThrowsUnauthorized()
        {
            _mockCurrentUser.Setup(c => c.StudentId).Returns((Guid?)null);

            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _service.GetStudentSchedulesAsync(new ScheduleFilter()));
            Assert.That(ex.Message, Does.Contain("Student ID is missing"));
        }

        [Test]
        public void GetStudentSchedulesAsync_FromDateGreaterThanToDate_ThrowsArgumentException()
        {
            _mockCurrentUser.Setup(c => c.StudentId).Returns(Guid.NewGuid());

            var filter = new ScheduleFilter
            {
                FromDate = new DateTime(2024, 12, 31),
                ToDate = new DateTime(2024, 1, 1)
            };

            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _service.GetStudentSchedulesAsync(filter));
            Assert.That(ex.Message, Does.Contain("Ngày bắt đầu phải trước ngày kết thúc"));
        }

        [Test]
        public async Task GetStudentSchedulesAsync_ValidFilter_CalculatesStatusesCorrectly()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var filter = new ScheduleFilter { 
                FromDate = new DateTime(2020, 1, 1),
                ToDate = new DateTime(2030, 1, 1) 
            };

            // Cấu hình thời gian chuẩn để test logic (UTC+7)
            var nowUtc7 = DateTime.UtcNow.AddHours(7);
            var nowDate = DateOnly.FromDateTime(nowUtc7);
            var nowTime = TimeOnly.FromDateTime(nowUtc7);

            var mockTuples = new List<(Session Session, Attendance Attendance)>
            {
                // 1. Tương lai (Ngày mai) -> "Sắp diễn ra", Attendance Null -> "N/A"
                (
                    new Session { SessionId = Guid.NewGuid(), Date = nowDate.AddDays(1) },
                    null
                ),

                // 2. Quá khứ (Hôm qua), Đã điểm danh (Present) -> "Đã kết thúc", "Có mặt"
                (
                    new Session { SessionId = Guid.NewGuid(), Date = nowDate.AddDays(-1) },
                    new Attendance { Status = "Present" }
                ),

                // 3. Hôm nay, Đang trong giờ học (Start < Now < End) -> "Đang diễn ra", Attendance Null -> "N/A"
                (
                    new Session
                    {
                        SessionId = Guid.NewGuid(),
                        Date = nowDate,
                        StartTime = nowTime.AddHours(-1),
                        EndTime = nowTime.AddHours(1)
                    },
                    null
                ),

                // 4. Hôm nay, Đã qua giờ học (Now > End) -> "Đã kết thúc", Attendance Null -> "Chưa điểm danh"
                (
                    new Session
                    {
                        SessionId = Guid.NewGuid(),
                        Date = nowDate,
                        StartTime = nowTime.AddHours(-4),
                        EndTime = nowTime.AddHours(-2)
                    },
                    null
                )
            };

            _mockSessionRepo.Setup(r => r.GetStudentSchedulesAsync(studentId, filter.FromDate, filter.ToDate, filter.ClassID))
                            .ReturnsAsync(mockTuples);

            // Act
            var result = (await _service.GetStudentSchedulesAsync(filter)).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(4));

            // Case 1: Tương lai
            Assert.That(result[0].Status, Is.EqualTo("Sắp diễn ra"));
            Assert.That(result[0].AttendanceStatus, Is.EqualTo("N/A"));

            // Case 2: Quá khứ + Có điểm danh
            Assert.That(result[1].Status, Is.EqualTo("Đã kết thúc"));
            Assert.That(result[1].AttendanceStatus, Is.EqualTo("Có mặt"));

            // Case 3: Đang diễn ra
            Assert.That(result[2].Status, Is.EqualTo("Đang diễn ra"));
            Assert.That(result[2].AttendanceStatus, Is.EqualTo("N/A"));

            // Case 4: Quá khứ trong ngày + Không điểm danh
            Assert.That(result[3].Status, Is.EqualTo("Đã kết thúc"));
            Assert.That(result[3].AttendanceStatus, Is.EqualTo("Chưa điểm danh"));
        }

        #endregion
    }
}