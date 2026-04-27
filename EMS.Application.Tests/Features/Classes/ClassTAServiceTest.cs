using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Classes.Services;
using EMS.Application.Features.Notifications.Services;
using EMS.Application.Features.Assignments.Services; // Vì Logger dùng chung type này trong code của bạn
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;

namespace EMS.Application.Tests.Features.Classes
{
    [TestFixture]
    public class ClassTAServiceTests
    {
        private Mock<IClassRepository> _mockClassRepo;
        private Mock<ITARepository> _mockTaRepo;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<ILogger<AssignmentService>> _mockLogger;
        private Mock<ICurrentUserService> _mockCurrentUser;
        private ClassTAService _service;

        [SetUp]
        public void Setup()
        {
            _mockClassRepo = new Mock<IClassRepository>();
            _mockTaRepo = new Mock<ITARepository>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<AssignmentService>>();
            _mockCurrentUser = new Mock<ICurrentUserService>();

            _service = new ClassTAService(
                _mockClassRepo.Object,
                _mockTaRepo.Object,
                _mockNotificationService.Object,
                _mockLogger.Object,
                _mockCurrentUser.Object
            );
        }

        #region 1. AssignTAAsync Tests

        [Test]
        public void AssignTAAsync_ClassNotFound_ThrowsException()
        {
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Class)null);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.AssignTAAsync(Guid.NewGuid(), new AssignTADto()));
            Assert.That(ex.Message, Is.EqualTo("Lớp học không tồn tại"));
        }

        [Test]
        public void AssignTAAsync_ClassCompletedOrArchived_ThrowsException()
        {
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
            var classObj = new Class { Status = "Completed" };
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(classObj);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.AssignTAAsync(Guid.NewGuid(), new AssignTADto()));
            Assert.That(ex.Message, Does.Contain("đã hoàn thành hoặc lưu trữ"));
        }

        [Test]
        public void AssignTAAsync_Unauthorized_ThrowsException()
        {
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid()); // Khác TeacherId
            _mockCurrentUser.Setup(c => c.Role).Returns("Student");
            var classObj = new Class { Status = "Active", TeacherId = Guid.NewGuid() };
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(classObj);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.AssignTAAsync(Guid.NewGuid(), new AssignTADto()));
            Assert.That(ex.Message, Does.Contain("không có quyền thao tác"));
        }

        [Test]
        public async Task AssignTAAsync_ExistingDeactiveTA_ReactivatesAndNotifies()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");

            var classObj = new Class { Status = "Active", TeacherId = teacherId, ClassName = "C# Basic" };
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(classObj);

            var existingTA = new ClassTum { Status = "Deactive", ClassTaid = Guid.NewGuid() };
            _mockClassRepo.Setup(r => r.GetClassTAAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(existingTA);

            var request = new AssignTADto { TAID = Guid.NewGuid(), Permission = "Grade", SalaryPerSession = 100000 };

            var result = await _service.AssignTAAsync(Guid.NewGuid(), request);

            Assert.That(existingTA.Status, Is.EqualTo("Active"));
            Assert.That(existingTA.Permission, Is.EqualTo("Grade"));
            _mockClassRepo.Verify(r => r.UpdateClassTAAsync(existingTA), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(request.TAID, null, "Phân công lớp mới", It.IsAny<string>(), It.IsAny<string>(), "Class"), Times.Once);
        }

        [Test]
        public async Task AssignTAAsync_NewTA_AddsAndNotifies()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");

            var classObj = new Class { Status = "Active", TeacherId = teacherId };
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(classObj);
            _mockClassRepo.Setup(r => r.GetClassTAAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((ClassTum)null);

            var request = new AssignTADto { TAID = Guid.NewGuid(), Permission = "Grade" };

            var result = await _service.AssignTAAsync(Guid.NewGuid(), request);

            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _mockClassRepo.Verify(r => r.AddClassTAAsync(It.Is<ClassTum>(c => c.Status == "Active" && c.Permission == "Grade")), Times.Once);
        }

        #endregion

        #region 2. GetClassTAsAsync Tests

        [Test]
        public void GetClassTAsAsync_Unauthorized_ThrowsException()
        {
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
            _mockCurrentUser.Setup(c => c.Role).Returns("Student");
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class { TeacherId = Guid.NewGuid() });

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.GetClassTAsAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("không phải là giảng viên"));
        }

        [Test]
        public async Task GetClassTAsAsync_Valid_ReturnsMappedList()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class { TeacherId = teacherId });

            var classTAs = new List<ClassTum>
            {
                new ClassTum
                {
                    Taid = Guid.NewGuid(), Permission = "Grade", Status = "Active",
                    Ta = new TeachingAssistant { Ta = new Account { FullName = "TA 1", Email = "ta1@test.com" } }
                }
            };
            _mockClassRepo.Setup(r => r.GetTAsByClassIdAsync(It.IsAny<Guid>())).ReturnsAsync(classTAs);

            var result = await _service.GetClassTAsAsync(Guid.NewGuid());

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().FullName, Is.EqualTo("TA 1"));
        }

        #endregion

        #region 3. UpdateTAPermissionAsync Tests

        [Test]
        public void UpdateTAPermissionAsync_TAEntityNotFound_ThrowsException()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class { TeacherId = teacherId });
            _mockTaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TeachingAssistant)null);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.UpdateTAPermissionAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateTAPermissionDto()));
            Assert.That(ex.Message, Does.Contain("Không tìm thấy trợ giảng trong hệ thống"));
        }

        [Test]
        public async Task UpdateTAPermissionAsync_Valid_UpdatesPermission()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");

            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class { TeacherId = teacherId });
            _mockTaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new TeachingAssistant());

            var classTa = new ClassTum { Status = "Active", Permission = "Old" };
            _mockClassRepo.Setup(r => r.GetClassTAAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(classTa);

            await _service.UpdateTAPermissionAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateTAPermissionDto { Permission = "NewPermission" });

            Assert.That(classTa.Permission, Is.EqualTo("NewPermission"));
            _mockClassRepo.Verify(r => r.UpdateClassTAAsync(classTa), Times.Once);
        }

        #endregion

        #region 4. CreateTaskAsync Tests

        [Test]
        public void CreateTaskAsync_NoPermissionForType_ThrowsException()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");

            var classTA = new ClassTum { Permission = "Grade", Class = new Class { TeacherId = teacherId } };
            _mockTaRepo.Setup(r => r.GetClassTAByIdAsync(It.IsAny<Guid>())).ReturnsAsync(classTA);

            var request = new CreateTaskDto { Title = "Title", DueDate = DateTime.Now.AddDays(1), Type = "Attendance" }; // Type không có trong Permission

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.CreateTaskAsync(request));
            Assert.That(ex.Message, Does.Contain("không có quyền thực hiện nhiệm vụ loại"));
        }

        [Test]
        public async Task CreateTaskAsync_Valid_CreatesAndNotifies()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");

            var classTA = new ClassTum { Permission = "Grade, Attendance", Class = new Class { TeacherId = teacherId, ClassName = "Lớp A" } };
            _mockTaRepo.Setup(r => r.GetClassTAByIdAsync(It.IsAny<Guid>())).ReturnsAsync(classTA);
            _mockNotificationService.Setup(n => n.GetTAAccountInfoByClassTaidAsync(It.IsAny<Guid>()))
                                    .ReturnsAsync((Guid.NewGuid(), "Lớp A"));

            var request = new CreateTaskDto { Title = "Task 1", DueDate = DateTime.Now.AddDays(1), Type = "Grade" };

            var result = await _service.CreateTaskAsync(request);

            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _mockTaRepo.Verify(r => r.CreateTaskAsync(It.Is<TeachingAssistantTask>(t => t.Type == "Grade" && t.Status == "Todo")), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), null, "Nhiệm vụ mới", It.IsAny<string>(), It.IsAny<string>(), "Task"), Times.Once);
        }

        #endregion

        #region 5. GetTAsByTeacherIdAsync Tests

        [Test]
        public async Task GetTAsByTeacherIdAsync_Valid_ReturnsMappedList()
        {
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
            var tas = new List<ClassTum>
            {
                new ClassTum
                {
                    Class = new Class { ClassName = "Lớp B" },
                    Ta = new TeachingAssistant { Ta = new Account { FullName = "TA X" } }
                }
            };
            _mockTaRepo.Setup(r => r.GetTAsByTeacherIdAsync(It.IsAny<Guid>())).ReturnsAsync(tas);

            var result = await _service.GetTAsByTeacherIdAsync();

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().ClassName, Is.EqualTo("Lớp B"));
        }

        #endregion

        #region 6. FindTAByEmailAsync Tests

        [Test]
        public void FindTAByEmailAsync_EmailNull_ThrowsArgumentNullException()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _service.FindTAByEmailAsync(null));
        }

        [Test]
        public void FindTAByEmailAsync_NotTARole_ThrowsException()
        {
            var entity = new TeachingAssistant { Ta = new Account { Role = new Role { RoleName = "Student" } } };
            _mockTaRepo.Setup(r => r.GetTAByEmailAsync("test@test.com")).ReturnsAsync(entity);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.FindTAByEmailAsync("test@test.com"));
            Assert.That(ex.Message, Does.Contain("không phải là trợ giảng"));
        }

        [Test]
        public async Task FindTAByEmailAsync_Valid_ReturnsProfile()
        {
            var entity = new TeachingAssistant { Ta = new Account { FullName = "Nguyễn TA", Role = new Role { RoleName = "TA" } } };
            _mockTaRepo.Setup(r => r.GetTAByEmailAsync("ta@test.com")).ReturnsAsync(entity);

            var result = await _service.FindTAByEmailAsync("ta@test.com");

            Assert.That(result.FullName, Is.EqualTo("Nguyễn TA"));
        }

        #endregion

        #region 7. RemoveTAFromClassAsync Tests

        [Test]
        public void RemoveTAFromClassAsync_ClassTANotFound_ThrowsException()
        {
            _mockClassRepo.Setup(r => r.GetClassTAAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((ClassTum)null);
            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.RemoveTAFromClassAsync(Guid.NewGuid(), Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("Không tìm thấy trợ giảng"));
        }

        [Test]
        public async Task RemoveTAFromClassAsync_Valid_UpdatesAndNotifies()
        {
            var classTa = new ClassTum { Status = "Active" };
            _mockClassRepo.Setup(r => r.GetClassTAAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(classTa);
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Class { ClassName = "Lớp C" });

            var result = await _service.RemoveTAFromClassAsync(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result, Is.True);
            Assert.That(classTa.Status, Is.EqualTo("Deactive"));
            _mockClassRepo.Verify(r => r.UpdateClassTAAsync(classTa), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), null, "Ngừng phân công", It.IsAny<string>(), It.IsAny<string>(), "Class"), Times.Once);
        }

        #endregion

        #region 8. UpdateTaskStatusAsync Tests

        [Test]
        public void UpdateTaskStatusAsync_UnauthorizedTA_ThrowsException()
        {
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
            var task = new TeachingAssistantTask { ClassTa = new ClassTum { Taid = Guid.NewGuid() } }; // ID khác
            _mockTaRepo.Setup(r => r.GetTaskByIdAsync(It.IsAny<Guid>())).ReturnsAsync(task);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.UpdateTaskStatusAsync(Guid.NewGuid(), new UpdateTaskStatusDto()));
            Assert.That(ex.Message, Does.Contain("Bạn không có quyền thao tác"));
        }

        [Test]
        public void UpdateTaskStatusAsync_InvalidTransition_ThrowsException()
        {
            var taId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(taId);

            // Cố tình chuyển từ Todo thẳng sang Review (Không hợp lệ)
            var task = new TeachingAssistantTask { Status = "Todo", ClassTa = new ClassTum { Taid = taId } };
            _mockTaRepo.Setup(r => r.GetTaskByIdAsync(It.IsAny<Guid>())).ReturnsAsync(task);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.UpdateTaskStatusAsync(Guid.NewGuid(), new UpdateTaskStatusDto { Status = "Review" }));
            Assert.That(ex.Message, Does.Contain("không hợp lệ"));
        }

        [Test]
        public async Task UpdateTaskStatusAsync_ToReview_UpdatesAndNotifiesTeacher()
        {
            var taId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(taId);

            var task = new TeachingAssistantTask
            {
                Status = "InProgress",
                ClassTa = new ClassTum { Taid = taId, Class = new Class { TeacherId = Guid.NewGuid(), ClassName = "Class" } }
            };
            _mockTaRepo.Setup(r => r.GetTaskByIdAsync(It.IsAny<Guid>())).ReturnsAsync(task);

            await _service.UpdateTaskStatusAsync(Guid.NewGuid(), new UpdateTaskStatusDto { Status = "Review" });

            Assert.That(task.Status, Is.EqualTo("Review"));
            _mockTaRepo.Verify(r => r.UpdateTaskAsync(task), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), null, "Nhiệm vụ chờ duyệt", It.IsAny<string>(), It.IsAny<string>(), "Task"), Times.Once);
        }

        #endregion

        #region 9. ReviewTaskAsync Tests

        [Test]
        public void ReviewTaskAsync_NotReviewStatus_ThrowsException()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            var task = new TeachingAssistantTask { Status = "InProgress", ClassTa = new ClassTum { Class = new Class { TeacherId = teacherId } } };
            _mockTaRepo.Setup(r => r.GetTaskByIdAsync(It.IsAny<Guid>())).ReturnsAsync(task);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.ReviewTaskAsync(Guid.NewGuid(), true, "Good"));
            Assert.That(ex.Message, Does.Contain("Không thể thực hiện thao tác này"));
        }

        [Test]
        public async Task ReviewTaskAsync_Approved_UpdatesToDoneAndNotifies()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            var task = new TeachingAssistantTask { Status = "Review", ClassTa = new ClassTum { Taid = Guid.NewGuid(), Class = new Class { TeacherId = teacherId } } };
            _mockTaRepo.Setup(r => r.GetTaskByIdAsync(It.IsAny<Guid>())).ReturnsAsync(task);

            await _service.ReviewTaskAsync(Guid.NewGuid(), true, "Perfect");

            Assert.That(task.Status, Is.EqualTo("Done"));
            Assert.That(task.Feedback, Is.EqualTo("Perfect"));
            _mockTaRepo.Verify(r => r.UpdateTaskAsync(task), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), null, "Nhiệm vụ hoàn tất", It.Is<string>(s => s.Contains("đã được duyệt")), It.IsAny<string>(), "Task"), Times.Once);
        }

        [Test]
        public async Task ReviewTaskAsync_Rejected_UpdatesToRejectedAndNotifies()
        {
            var teacherId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            var task = new TeachingAssistantTask { Status = "Review", ClassTa = new ClassTum { Taid = Guid.NewGuid(), Class = new Class { TeacherId = teacherId } } };
            _mockTaRepo.Setup(r => r.GetTaskByIdAsync(It.IsAny<Guid>())).ReturnsAsync(task);

            await _service.ReviewTaskAsync(Guid.NewGuid(), false, "Need rework");

            Assert.That(task.Status, Is.EqualTo("Rejected"));
            Assert.That(task.Feedback, Is.EqualTo("Need rework"));
            _mockTaRepo.Verify(r => r.UpdateTaskAsync(task), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), null, "Nhiệm vụ cần sửa lại", It.Is<string>(s => s.Contains("cần được chỉnh sửa")), It.IsAny<string>(), "Task"), Times.Once);
        }

        #endregion
    }
}