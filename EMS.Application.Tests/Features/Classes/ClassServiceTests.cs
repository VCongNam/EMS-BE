using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using FluentValidation;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Classes.Services;
using EMS.Application.Features.Notifications.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;

namespace EMS.Application.Tests.Features.Classes
{
    [TestFixture]
    public class ClassServiceTests
    {
        private Mock<IClassRepository> _mockClassRepo;
        private Mock<ISessionRepository> _mockSessionRepo;
        private Mock<ICurrentUserService> _mockCurrentUser;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<IValidator<CreateClassDto>> _mockValidator;
        private ClassService _service;

        [SetUp]
        public void Setup()
        {
            _mockClassRepo = new Mock<IClassRepository>();
            _mockSessionRepo = new Mock<ISessionRepository>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockValidator = new Mock<IValidator<CreateClassDto>>();

            _service = new ClassService(
                _mockClassRepo.Object,
                _mockSessionRepo.Object,
                _mockCurrentUser.Object,
                _mockNotificationService.Object,
                _mockValidator.Object
            );
        }

        #region 1. CreateClassAsync Tests

        [Test]
        public void CreateClassAsync_ScheduleOverlaps_ThrowsBadRequestException()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);

            var request = new CreateClassDto
            {
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(1)),
                Schedules = new List<ScheduleDto>
                {
                    new ScheduleDto { DayOfWeek = 2, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0) } // Thứ 3, 8h-10h
                }
            };

            var existingClass = new Class
            {
                Status = "Active",
                ClassName = "Lớp cũ",
                StartDate = request.StartDate.AddDays(-10),
                EndDate = request.EndDate.AddDays(10),
                ClassSchedules = new List<ClassSchedule>
                {
                    new ClassSchedule { DayOfWeek = 2, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) } // Trùng giờ (9h-11h đè vào 8h-10h)
                }
            };

            _mockClassRepo.Setup(r => r.GetClassesByTeacherIdAsync(teacherId)).ReturnsAsync(new List<Class> { existingClass });

            // Act & Assert
            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.CreateClassAsync(request));
            Assert.That(ex.Message, Does.Contain("Trùng lịch dạy!"));
        }

        [Test]
        public async Task CreateClassAsync_ValidData_CreatesSubjectAndClassAndSessions()
        {
            // Arrange
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
            var request = new CreateClassDto
            {
                SubjectName = "Toán",
                GradeLevel = 10,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7)), // Học trong 1 tuần
                Schedules = new List<ScheduleDto>
                {
                    new ScheduleDto { DayOfWeek = (short)DateTime.UtcNow.DayOfWeek, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0) }
                }
            };

            _mockClassRepo.Setup(r => r.GetClassesByTeacherIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Class>());
            _mockClassRepo.Setup(r => r.GetSubjectByNameAndGradeAsync("Toán", 10)).ReturnsAsync((Subject)null); // Subject chưa có

            // Act
            var result = await _service.CreateClassAsync(request);

            // Assert
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _mockClassRepo.Verify(r => r.AddSubjectAsync(It.Is<Subject>(s => s.SubjectName == "Toán")), Times.Once);
            _mockClassRepo.Verify(r => r.AddAsync(It.Is<Class>(c => c.Status == "Scheduled" && c.ClassSchedules.Count == 1 && c.Sessions.Count > 0)), Times.Once);
        }

        #endregion

        #region 2. UpdateClassAsync Tests

        [Test]
        public void UpdateClassAsync_ClassNotFound_ThrowsException()
        {
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Class)null);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.UpdateClassAsync(Guid.NewGuid(), new UpdateClassDto()));
            Assert.That(ex.Message, Does.Contain("not found"));
        }

        [Test]
        public void UpdateClassAsync_MaxStudentsLessThanActive_ThrowsBadRequestException()
        {
            var classId = Guid.NewGuid();
            _mockClassRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Class());
            _mockClassRepo.Setup(r => r.GetActiveStudentCountAsync(classId)).ReturnsAsync(20); // Đang có 20 học sinh

            var request = new UpdateClassDto { MaxStudents = 15 }; // Hạ xuống 15

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.UpdateClassAsync(classId, request));
            Assert.That(ex.Message, Does.Contain("Khong the cap nhat si so toi da xuong 15"));
        }

        [Test]
        public async Task UpdateClassAsync_ValidRequest_UpdatesClassAndRecreatesSessions()
        {
            var classId = Guid.NewGuid();
            _mockClassRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Class { ClassId = classId });
            _mockClassRepo.Setup(r => r.GetActiveStudentCountAsync(classId)).ReturnsAsync(10);
            _mockClassRepo.Setup(r => r.GetSubjectByNameAndGradeAsync(It.IsAny<string>(), It.IsAny<short>())).ReturnsAsync(new Subject { SubjectId = Guid.NewGuid() });

            // Giả lập 1 session ở tương lai để test hàm xóa session cũ
            var futureSession = new Session { Status = "Scheduled", Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) };
            _mockSessionRepo.Setup(r => r.GetSessionsByClassIdAsync(classId)).ReturnsAsync(new List<Session> { futureSession });

            var request = new UpdateClassDto
            {
                ClassName = "Lớp Update",
                MaxStudents = 20,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                Schedules = new List<ScheduleDto> { new ScheduleDto { DayOfWeek = 2, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0) } }
            };

            await _service.UpdateClassAsync(classId, request);

            _mockClassRepo.Verify(r => r.UpdateAsync(It.Is<Class>(c => c.ClassName == "Lớp Update")), Times.Once);
            _mockClassRepo.Verify(r => r.DeleteSchedulesAsync(classId), Times.Once);
            _mockClassRepo.Verify(r => r.AddSchedulesAsync(It.IsAny<IEnumerable<ClassSchedule>>()), Times.Once);
            _mockSessionRepo.Verify(r => r.DeleteSessionsAsync(It.Is<List<Session>>(l => l.Contains(futureSession))), Times.Once);
            _mockSessionRepo.Verify(r => r.AddSessionsAsync(It.IsAny<List<Session>>()), Times.Once);
        }

        #endregion

        #region 3. ArchiveClassAsync Tests

        [Test]
        public void ArchiveClassAsync_ClassNotFound_ThrowsException()
        {
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Class)null);
            Assert.ThrowsAsync<Exception>(async () => await _service.ArchiveClassAsync(Guid.NewGuid()));
        }

        [Test]
        public async Task ArchiveClassAsync_Valid_UpdatesStatusToArchived()
        {
            var classroom = new Class { Status = "Active" };
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(classroom);

            await _service.ArchiveClassAsync(Guid.NewGuid());

            Assert.That(classroom.Status, Is.EqualTo("Archived"));
            _mockClassRepo.Verify(r => r.UpdateAsync(classroom), Times.Once);
        }

        #endregion

        #region 4. AssignStudentAsync Tests

        [Test]
        public void AssignStudentAsync_Unauthorized_ThrowsException()
        {
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid()); // Khác TeacherId
            _mockCurrentUser.Setup(c => c.Role).Returns("Student");
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class { TeacherId = Guid.NewGuid() });

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.AssignStudentAsync(Guid.NewGuid(), new AssignStudentDto()));
            Assert.That(ex.Message, Does.Contain("Bạn không có quyền thao tác"));
        }

        [Test]
        public void AssignStudentAsync_MaxStudentsReached_ThrowsException()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");

            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class { TeacherId = teacherId, MaxStudents = 20 });
            _mockClassRepo.Setup(r => r.GetActiveStudentCountAsync(It.IsAny<Guid>())).ReturnsAsync(20);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.AssignStudentAsync(Guid.NewGuid(), new AssignStudentDto()));
            Assert.That(ex.Message, Does.Contain("Lớp học đã đạt số lượng tối đa"));
        }

        [Test]
        public async Task AssignStudentAsync_ExistingDroppedEnrollment_RestoresAndNotifies()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");

            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class { TeacherId = teacherId, MaxStudents = 20 });
            _mockClassRepo.Setup(r => r.GetActiveStudentCountAsync(It.IsAny<Guid>())).ReturnsAsync(10);
            _mockNotificationService.Setup(n => n.GetAccountIdByStudentIdAsync(It.IsAny<Guid>())).ReturnsAsync(Guid.NewGuid());

            var existingEnrollment = new ClassEnrollment { Status = "Dropped" };
            _mockClassRepo.Setup(r => r.GetEnrollmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(existingEnrollment);

            var request = new AssignStudentDto { StudentID = Guid.NewGuid() };
            var result = await _service.AssignStudentAsync(Guid.NewGuid(), request);

            Assert.That(result, Is.True);
            Assert.That(existingEnrollment.Status, Is.EqualTo("Active"));
            _mockClassRepo.Verify(r => r.UpdateEnrollment(existingEnrollment), Times.Once);
            _mockClassRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), request.StudentID, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "Class"), Times.Once);
        }

        [Test]
        public async Task AssignStudentAsync_NewEnrollment_AddsAndNotifies()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");

            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class { TeacherId = teacherId, MaxStudents = 20 });
            _mockClassRepo.Setup(r => r.GetActiveStudentCountAsync(It.IsAny<Guid>())).ReturnsAsync(10);
            _mockNotificationService.Setup(n => n.GetAccountIdByStudentIdAsync(It.IsAny<Guid>())).ReturnsAsync(Guid.NewGuid());
            _mockClassRepo.Setup(r => r.GetEnrollmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((ClassEnrollment)null);

            var result = await _service.AssignStudentAsync(Guid.NewGuid(), new AssignStudentDto { StudentID = Guid.NewGuid() });

            Assert.That(result, Is.True);
            _mockClassRepo.Verify(r => r.AddEnrollmentAsync(It.Is<ClassEnrollment>(e => e.Status == "Active")), Times.Once);
            _mockClassRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region 5. RemoveStudentFromClassAsync Tests

        [Test]
        public void RemoveStudentFromClassAsync_ClassArchived_ThrowsException()
        {
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class { Status = "Archived" });

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.RemoveStudentFromClassAsync(Guid.NewGuid(), Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("đã kết thúc hoặc lưu trữ"));
        }

        [Test]
        public async Task RemoveStudentFromClassAsync_Valid_DropsStudent()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");

            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class { TeacherId = teacherId, Status = "Active" });

            var enrollment = new ClassEnrollment { Status = "Active" };
            _mockClassRepo.Setup(r => r.GetEnrollmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(enrollment);

            var result = await _service.RemoveStudentFromClassAsync(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result, Is.True);
            Assert.That(enrollment.Status, Is.EqualTo("Dropped"));
            _mockClassRepo.Verify(r => r.UpdateEnrollment(enrollment), Times.Once);
            _mockClassRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region 6. RestoreStudentInClassAsync Tests

        [Test]
        public void RestoreStudentInClassAsync_EnrollmentActive_ThrowsException()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");

            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class { TeacherId = teacherId, Status = "Active" });
            _mockClassRepo.Setup(r => r.GetEnrollmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(new ClassEnrollment { Status = "Active" });

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.RestoreStudentInClassAsync(Guid.NewGuid(), Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("vẫn đang học bình thường"));
        }

        [Test]
        public async Task RestoreStudentInClassAsync_Valid_RestoresStudent()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");

            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class { TeacherId = teacherId, Status = "Active" });

            var enrollment = new ClassEnrollment { Status = "Dropped" };
            _mockClassRepo.Setup(r => r.GetEnrollmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(enrollment);

            var result = await _service.RestoreStudentInClassAsync(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result, Is.True);
            Assert.That(enrollment.Status, Is.EqualTo("Active"));
            _mockClassRepo.Verify(r => r.UpdateEnrollment(enrollment), Times.Once);
            _mockClassRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region 7. GetTeacherDashboardAsync Tests

        [Test]
        public async Task GetTeacherDashboardAsync_ReturnsMappedDataAndFiltersArchived()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);

            var classes = new List<Class>
            {
                new Class {
                    ClassName = "Lớp 1",
                    Status = "Active",
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                    EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                    ClassEnrollments = new List<ClassEnrollment>(),
                    ClassSchedules = new List<ClassSchedule>() },
                new Class { ClassName = "Lớp Archived", Status = "Archived" } // Bị bỏ qua
            };
            _mockClassRepo.Setup(r => r.GetClassesByTeacherIdAsync(teacherId)).ReturnsAsync(classes);

            var result = await _service.GetTeacherDashboardAsync();

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().ClassName, Is.EqualTo("Lớp 1"));
            Assert.That(result.First().Status, Is.EqualTo("Ongoing")); // Đã qua ngày StartDate
        }

        #endregion

        #region 8. GetClassDetailAsync Tests

        [Test]
        public void GetClassDetailAsync_ClassNotFound_ThrowsException()
        {
            _mockClassRepo.Setup(r => r.GetClassDetailByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Class)null);
            Assert.ThrowsAsync<Exception>(async () => await _service.GetClassDetailAsync(Guid.NewGuid()));
        }

        [Test]
        public async Task GetClassDetailAsync_Valid_ReturnsMappedDto()
        {
            var classId = Guid.NewGuid();
            var classroom = new Class
            {
                ClassId = classId,
                ClassName = "Test Class",
                CreatedAt = DateTime.UtcNow,
                ClassEnrollments = new List<ClassEnrollment> { new ClassEnrollment { Status = "Active" } },
                ClassSchedules = new List<ClassSchedule> { new ClassSchedule { DayOfWeek = 2, StartTime = new TimeOnly(8, 0) } }
            };
            _mockClassRepo.Setup(r => r.GetClassDetailByIdAsync(classId)).ReturnsAsync(classroom);

            var result = await _service.GetClassDetailAsync(classId);

            Assert.That(result.ClassId, Is.EqualTo(classId));
            Assert.That(result.ClassName, Is.EqualTo("Test Class"));
            Assert.That(result.CurrentStudents, Is.EqualTo(1));
            Assert.That(result.Schedules.Count, Is.EqualTo(1));
        }

        #endregion
    }
}