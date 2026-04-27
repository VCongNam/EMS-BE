using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using EMS.Application.Features.Sessions.Services;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Notifications.Services;
using EMS.Application.Features.Sessions.DTOs;
using EMS.Application.Features.Assignments.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;

namespace EMS.Application.Tests.Features.Sessions
{
    [TestFixture]
    public class SessionServiceTests
    {
        private Mock<ISessionRepository> _mockSessionRepo;
        private Mock<IClassRepository> _mockClassRepo;
        private Mock<ICurrentUserService> _mockCurrentUser;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<ILogger<AssignmentService>> _mockLogger;
        private SessionService _service;

        [SetUp]
        public void Setup()
        {
            _mockSessionRepo = new Mock<ISessionRepository>();
            _mockClassRepo = new Mock<IClassRepository>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<AssignmentService>>();

            _service = new SessionService(
                _mockSessionRepo.Object,
                _mockClassRepo.Object,
                _mockCurrentUser.Object,
                _mockNotificationService.Object,
                _mockLogger.Object
            );
        }

        #region 1. CreateSessionAsync Tests

        [Test]
        public void CreateSessionAsync_ClassNotFound_ThrowsException()
        {
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Class)null);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.CreateSessionAsync(new CreateSessionDto { ClassId = Guid.NewGuid() }));
            Assert.That(ex.Message, Does.Contain("not found"));
        }

        [Test]
        public void CreateSessionAsync_StartTimeAfterEndTime_ThrowsException()
        {
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class());
            var request = new CreateSessionDto
            {
                ClassId = Guid.NewGuid(),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(8, 0) // Lỗi logic thời gian
            };

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.CreateSessionAsync(request));
            Assert.That(ex.Message, Does.Contain("Thời gian bắt đầu phải trước thời gian kết thúc"));
        }

        [Test]
        public void CreateSessionAsync_TimeConflict_ThrowsException()
        {
            var teacherId = Guid.NewGuid();
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class { TeacherId = teacherId });

            // Giả lập lịch dạy hiện tại bị trùng giờ (9h-11h đè vào 8h-10h)
            var conflictingSessions = new List<Session>
            {
                new Session { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }
            };
            _mockSessionRepo.Setup(r => r.GetSessionsByTeacherAndDateAsync(teacherId, It.IsAny<DateOnly>(), null))
                            .ReturnsAsync(conflictingSessions);

            var request = new CreateSessionDto { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0) };

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.CreateSessionAsync(request));
            Assert.That(ex.Message, Does.Contain("Lịch học bị trùng"));
        }

        [Test]
        public async Task CreateSessionAsync_ValidData_CreatesAndNotifies()
        {
            var teacherId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            _mockClassRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Class { TeacherId = teacherId, ClassName = "Math" });

            // Không trùng lịch
            _mockSessionRepo.Setup(r => r.GetSessionsByTeacherAndDateAsync(teacherId, It.IsAny<DateOnly>(), null))
                            .ReturnsAsync(new List<Session>());

            var request = new CreateSessionDto { ClassId = classId, Title = "Session 1", Date = DateOnly.FromDateTime(DateTime.Now) };

            _mockNotificationService.Setup(n => n.GetAllClassTargetsAsync(classId)).ReturnsAsync(new List<(Guid, Guid?)> { (Guid.NewGuid(), Guid.NewGuid()) });

            var result = await _service.CreateSessionAsync(request);

            Assert.That(result.Title, Is.EqualTo("Session 1"));
            _mockSessionRepo.Verify(r => r.AddSessionAsync(It.Is<Session>(s => s.Title == "Session 1")), Times.Once);
            _mockNotificationService.Verify(n => n.SendBulkNotificationWithStudentAsync(It.IsAny<List<(Guid, Guid?)>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "Schedule"), Times.Once);
        }

        #endregion

        #region 2. UpdateSessionAsync Tests

        [Test]
        public void UpdateSessionAsync_SessionNotFound_ThrowsException()
        {
            _mockSessionRepo.Setup(r => r.GetSessionByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Session)null);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.UpdateSessionAsync(Guid.NewGuid(), new UpdateSessionDto()));
            Assert.That(ex.Message, Does.Contain("not found"));
        }

        [Test]
        public async Task UpdateSessionAsync_ValidData_UpdatesAndNotifies()
        {
            var sessionId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var session = new Session { SessionId = sessionId, ClassId = classId, Title = "Old" };

            _mockSessionRepo.Setup(r => r.GetSessionByIdAsync(sessionId)).ReturnsAsync(session);
            _mockClassRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Class { TeacherId = Guid.NewGuid() });
            _mockSessionRepo.Setup(r => r.GetSessionsByTeacherAndDateAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), sessionId))
                            .ReturnsAsync(new List<Session>()); // Không trùng lịch

            _mockNotificationService.Setup(n => n.GetAllClassTargetsAsync(classId)).ReturnsAsync(new List<(Guid, Guid?)> { (Guid.NewGuid(), Guid.NewGuid()) });

            var request = new UpdateSessionDto { Title = "New", Date = DateOnly.FromDateTime(DateTime.Now) };

            var result = await _service.UpdateSessionAsync(sessionId, request);

            Assert.That(result.Title, Is.EqualTo("New"));
            Assert.That(session.Title, Is.EqualTo("New"));
            _mockSessionRepo.Verify(r => r.UpdateSessionAsync(session), Times.Once);
            _mockNotificationService.Verify(n => n.SendBulkNotificationWithStudentAsync(It.IsAny<List<(Guid, Guid?)>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "Schedule"), Times.Once);
        }

        #endregion

        #region 3. GetAttendanceListAsync Tests

        [Test]
        public void GetAttendanceListAsync_SessionNotFound_ThrowsException()
        {
            _mockSessionRepo.Setup(r => r.GetSessionByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Session)null);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.GetAttendanceListAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("not found"));
        }

        [Test]
        public async Task GetAttendanceListAsync_ValidSession_MapsCorrectly()
        {
            var sessionId = Guid.NewGuid();
            _mockSessionRepo.Setup(r => r.GetSessionByIdAsync(sessionId)).ReturnsAsync(new Session());

            var studentId1 = Guid.NewGuid();
            var studentId2 = Guid.NewGuid();

            var students = new List<ClassEnrollment>
            {
                new ClassEnrollment { StudentId = studentId1, Student = new Student { FullName = "A" } },
                new ClassEnrollment { StudentId = studentId2, Student = new Student { FullName = "B" } }
            };
            _mockSessionRepo.Setup(r => r.GetStudentsForSessionAsync(sessionId)).ReturnsAsync(students);

            // Student 1 đã có dữ liệu điểm danh, Student 2 chưa có
            var attendances = new List<Attendance>
            {
                new Attendance { StudentId = studentId1, Status = "Present" }
            };
            _mockSessionRepo.Setup(r => r.GetAttendancesBySessionIdAsync(sessionId)).ReturnsAsync(attendances);

            var result = (await _service.GetAttendanceListAsync(sessionId)).ToList();

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.First(r => r.StudentId == studentId1).Status, Is.EqualTo("Present"));
            Assert.That(result.First(r => r.StudentId == studentId2).Status, Is.EqualTo("Not Taken")); // Fallback logic
        }

        #endregion

        #region 4. TakeAttendanceBulkAsync Tests

        [Test]
        public void TakeAttendanceBulkAsync_SessionNotFound_ThrowsException()
        {
            _mockSessionRepo.Setup(r => r.GetSessionByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Session)null);
            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.TakeAttendanceBulkAsync(Guid.NewGuid(), new List<TakeAttendanceDto>()));
            Assert.That(ex.Message, Does.Contain("not found"));
        }

        [Test]
        public async Task TakeAttendanceBulkAsync_MixedData_UpdatesInsertsAndNotifies()
        {
            var sessionId = Guid.NewGuid();
            _mockSessionRepo.Setup(r => r.GetSessionByIdAsync(sessionId)).ReturnsAsync(new Session { Title = "Math 101" });

            var stdUpdateId = Guid.NewGuid();
            var stdInsertId = Guid.NewGuid();

            var existingAttendances = new List<Attendance> { new Attendance { StudentId = stdUpdateId, Status = "Absent" } };
            _mockSessionRepo.Setup(r => r.GetAttendancesBySessionIdAsync(sessionId)).ReturnsAsync(existingAttendances);

            // Mock Data để Notification không bị lỗi Null Reference
            var classStudents = new List<ClassEnrollment>
            {
                new ClassEnrollment { StudentId = stdUpdateId, Student = new Student { AccountId = Guid.NewGuid() } },
                new ClassEnrollment { StudentId = stdInsertId, Student = new Student { AccountId = Guid.NewGuid() } }
            };
            _mockSessionRepo.Setup(r => r.GetStudentsForSessionAsync(sessionId)).ReturnsAsync(classStudents);

            var requests = new List<TakeAttendanceDto>
            {
                new TakeAttendanceDto { StudentId = stdUpdateId, Status = "Present" },
                new TakeAttendanceDto { StudentId = stdInsertId, Status = "Present" }
            };

            await _service.TakeAttendanceBulkAsync(sessionId, requests);

            // Verify Update & Insert logic
            _mockSessionRepo.Verify(r => r.UpdateRangeAsync(It.Is<List<Attendance>>(l => l.Count == 1 && l[0].StudentId == stdUpdateId && l[0].Status == "Present")), Times.Once);
            _mockSessionRepo.Verify(r => r.AddAttendancesAsync(It.Is<List<Attendance>>(l => l.Count == 1 && l[0].StudentId == stdInsertId)), Times.Once);

            // Verify Notification
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), "Thông báo điểm danh", It.IsAny<string>(), It.IsAny<string>(), "Attendance"), Times.Exactly(2));
        }

        #endregion

        #region 5. UpdateAttendanceAsync Tests

        [Test]
        public void UpdateAttendanceAsync_RecordNotFound_ThrowsException()
        {
            _mockSessionRepo.Setup(r => r.GetAttendanceByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Attendance)null);
            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.UpdateAttendanceAsync(Guid.NewGuid(), new UpdateAttendanceDto()));
            Assert.That(ex.Message, Does.Contain("not found"));
        }

        [Test]
        public async Task UpdateAttendanceAsync_ValidRequest_UpdatesAndNotifies()
        {
            var attendanceId = Guid.NewGuid();
            var attendance = new Attendance
            {
                Status = "Absent",
                StudentId = Guid.NewGuid(),
                Student = new Student { AccountId = Guid.NewGuid() },
                Session = new Session { Title = "Session" }
            };

            _mockSessionRepo.Setup(r => r.GetAttendanceByIdAsync(attendanceId)).ReturnsAsync(attendance);

            var request = new UpdateAttendanceDto { Status = "Present", Note = "Late" };

            await _service.UpdateAttendanceAsync(attendanceId, request);

            Assert.That(attendance.Status, Is.EqualTo("Present"));
            Assert.That(attendance.Note, Is.EqualTo("Late"));
            _mockSessionRepo.Verify(r => r.UpdateAttendanceAsync(attendance), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), "Cập nhật điểm danh", It.IsAny<string>(), It.IsAny<string>(), "Attendance"), Times.Once);
        }

        #endregion
    }
}