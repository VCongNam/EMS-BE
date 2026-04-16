using DocumentFormat.OpenXml.Spreadsheet;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Assignments.Services;
using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Notifications.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.Services
{
    public class ClassTAService : IClassTAService
    {
        private readonly IClassRepository _classRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly ITARepository _taRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AssignmentService> _logger;

        public ClassTAService(IClassRepository classRepository, ITARepository tARepository, INotificationService notificationService, ILogger<AssignmentService> logger, ICurrentUserService currentUserService)
        {
            _classRepository = classRepository;
            _taRepository = tARepository;
            _notificationService = notificationService;
            _logger = logger;
            _currentUser = currentUserService;
        }
        public async Task<Guid> AssignTAAsync(Guid classId, AssignTADto request)
        {
            var existingClassTA = await _classRepository.GetClassTAAsync(classId, request.TAID);
            Guid newClassTaId;
            if (existingClassTA != null)
            {
                if (existingClassTA.Status == "Removed" || existingClassTA.Status == "Deactive")
                {
                    existingClassTA.Status = "Active";
                    existingClassTA.Permission = request.Permission;
                    existingClassTA.SalaryPerSession = request.SalaryPerSession;
                    existingClassTA.UpdatedAt = DateTime.UtcNow;

                    await _classRepository.UpdateClassTAAsync(existingClassTA);
                    newClassTaId = existingClassTA.ClassTaid;
                }
                else
                {
                    throw new Exception("Trợ giảng đã được phân công vào lớp này và đang hoạt động!");
                }
            }
            else
            {
                // Tạo mới hoàn toàn
                var newClassTA = new ClassTum
                {
                    ClassTaid = Guid.NewGuid(),
                    ClassId = classId,
                    Taid = request.TAID,
                    Permission = request.Permission,
                    SalaryPerSession = request.SalaryPerSession,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                };

                await _classRepository.AddClassTAAsync(newClassTA);
                newClassTaId = newClassTA.ClassTaid;
            }

            //Notification: 
            try
            {
                // 1. Lấy thông tin chi tiết (Cần tên lớp và AccountId của TA)
                var classObj = await _classRepository.GetByIdAsync(classId);

                if (classObj != null)
                {
                    await _notificationService.SendNotificationAsync(
                        targetAccountId: request.TAID,
                        studentId: null,
                        title: "Phân công lớp mới",
                        content: $"Bạn đã được phân công làm trợ giảng cho lớp '{classObj.ClassName}'.",
                        actionUrl: $"/tutor/classes/{classId}",
                        type: "Class"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi gửi thông báo cho trợ giảng: {ex.Message}");
            }

            return newClassTaId;
        }

        

        //View class TAs
        public async Task<IEnumerable<ClassTADto>> GetClassTAsAsync(Guid classId)
        {
            var tas = await _classRepository.GetTAsByClassIdAsync(classId);
            if(tas == null)
            {
                throw new Exception("lớp này chưa được phân công trợ giảng!");
            }
            return tas.Select(cta => new ClassTADto
            {
                TAID = cta.Taid,
                FullName = cta.Ta.Ta.FullName,
                Email = cta.Ta.Ta.Email,
                Permission = cta.Permission,
                SalaryPerSession = cta.SalaryPerSession,
                ClassTAId = cta.ClassTaid,
                Status = cta.Status
            }).ToList();
        }

        public async Task UpdateTAPermissionAsync(Guid classId, Guid taId, UpdateTAPermissionDto request)
        {
            var classTa = await _classRepository.GetClassTAAsync(classId, taId);
            if (classTa == null || classTa.Status == "Removed" || classTa.Status == "Deactive")
            {
                throw new Exception("Không tìm thấy trợ giảng này trong lớp (hoặc đã bị gỡ).");
            }

            classTa.Permission = request.Permission;
            classTa.UpdatedAt = DateTime.UtcNow;

            await _classRepository.UpdateClassTAAsync(classTa);
        }

        public async Task<Guid> CreateTaskAsync(CreateTaskDto request)
        {
            var classTA = await _taRepository.GetClassTAByIdAsync(request.ClassTAID);

            if (classTA == null)
            {
                throw new Exception("Thông tin trợ giảng trong lớp học không tồn tại.");
            }

            var allowedPermissions = classTA.Permission?.Split(',')
                                .Select(p => p.Trim())
                                .ToList() ?? new List<string>();

            bool hasPermission = allowedPermissions.Any(p =>
                p.Equals(request.Type, StringComparison.OrdinalIgnoreCase));

            if (!hasPermission)
            {
                throw new Exception($"Trợ giảng không có quyền thực hiện nhiệm vụ loại: {request.Type}. " +
                                    $"Quyền hiện tại: {classTA.Permission}");
            }

            var newTask = new TeachingAssistantTask
            {
                TataskId = Guid.NewGuid(),
                ClassTaid = request.ClassTAID,
                Title = request.Title,
                DueDate = request.DueDate,
                Status = "Todo",
                Type = request.Type,
                CreatedAt = DateTime.UtcNow
            };

            await _taRepository.CreateTaskAsync(newTask);

            //Notification
            try
            {
                var (taAccountId, className) = await _notificationService.GetTAAccountInfoByClassTaidAsync(request.ClassTAID);

                if (taAccountId != Guid.Empty)
                {
                    await _notificationService.SendNotificationAsync(
                        targetAccountId: taAccountId,
                        studentId: null,
                        title: "Nhiệm vụ mới",
                        content: $"Bạn vừa được giao nhiệm vụ: '{request.Title}' cho lớp {className}. Hạn hoàn thành: {request.DueDate:dd/MM/yyyy}.",
                        actionUrl: $" /ta/tasks/{newTask.TataskId}", 
                        type: "Task"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi gửi thông báo nhiệm vụ cho TA: {ex.Message}");
            }

            return newTask.TataskId;
        }

        public async Task<IEnumerable<TaskDto>> GetTasksAsync(Guid classTaId)
        {
            var tasks = await _taRepository.GetTasksByClassTAIdAsync(classTaId);
            return tasks.Select(t => new TaskDto
            {
                TATaskID = t.TataskId,
                Title = t.Title,
                DueDate = t.DueDate,
                Status = t.Status ?? "N/A",
                Type = t.Type ?? "N/A",
                Feedback = t.Feedback
            }).ToList();
        }

        public async Task<IEnumerable<TAViewDto>> GetTAsByTeacherIdAsync()
        {
            Guid teacherId = _currentUser.UserId;
            var tas = await _taRepository.GetTAsByTeacherIdAsync(teacherId);
            if (tas == null) throw new Exception("Bạn chưa có trợ giảng ở lớp nào!");
            var result = tas.Select(ct => new TAViewDto
            {
                TAId = ct.Taid,
                ClassId = ct.ClassId,
                ClassName = ct.Class.ClassName,
                Permission = ct.Permission,
                SalaryPerSession = ct.SalaryPerSession,
                FullName = ct.Ta.Ta.FullName,
                Email = ct.Ta.Ta.Email,
                PhoneNumber = ct.Ta.Ta.PhoneNumber,
                ClassTaId = ct.ClassTaid,
                Status = ct.Status
            }).ToList();
            return result;
        }

        public async Task<TAProfileDto?> FindTAByEmailAsync(string email)
        {
            if (email == null) throw new ArgumentNullException("Hãy thêm email để tìm trợ giảng!");
            var taEntity = await _taRepository.GetTAByEmailAsync(email);
            if (taEntity == null)
                throw new Exception("Không có trợ giảng nào có email này!");

            return new TAProfileDto
            {
                TAId = taEntity.Taid,
                FullName = taEntity.Ta.FullName,
                Email = taEntity.Ta.Email,
                PhoneNumber = taEntity.Ta.PhoneNumber,
                Bio = taEntity.Bio,
                AvatarURL = taEntity.Ta.AvatarUrl,
            };
        }

        public async Task<IEnumerable<TaskDto>> GetTasksByTAIdAsync(Guid taId)
        {
            var tasks = await _taRepository.GetTasksByTAIdAsync(taId);

            return tasks.Select(t => new TaskDto
            {
                TATaskID = t.TataskId,
                ClassID = t.ClassTa.ClassId,
                ClassName = t.ClassTa.Class.ClassName, // Gán tên lớp vào đây
                Title = t.Title,
                DueDate = t.DueDate,
                Status = t.Status ?? "N/A",
                Type = t.Type ?? "N/A",
                Feedback = t.Feedback
            }).ToList();
        }

        public async Task<IEnumerable<AssignedClassDto>> GetClassesByTAIdAsync(Guid taId)
        {
            var assignments = await _classRepository.GetClassesByTAIdAsync(taId);

            return assignments.Select(a => new AssignedClassDto
            {
                ClassID = a.ClassId,
                ClassName = a.Class.ClassName,
                SubjectName = a.Class.Subject?.SubjectName ?? "N/A",
                TeacherName = a.Class.Teacher?.TeacherNavigation?.FullName ?? "N/A",
                Status = a.Class.Status,

                // Đếm số học sinh có trạng thái Active
                StudentCount = a.Class.ClassEnrollments.Count(ce => ce.Status == "Active"),

                // Format lịch học (Ví dụ: "Monday (08:00-10:00)")
                Schedules = a.Class.ClassSchedules.Select(s =>
                    $"{s.DayOfWeek} ({s.StartTime:hh\\:mm}-{s.EndTime:hh\\:mm})").ToList(),

                CreatedAt = a.Class.CreatedAt ?? DateTime.MinValue, // Thời gian mở lớp

                Permission = a.Permission,
                SalaryPerSession = a.SalaryPerSession,
                ClassTaId = a.ClassTaid,
            }).ToList();
        }

        public async Task<bool> RemoveTAFromClassAsync(Guid classId, Guid taId)
        {
            var classTA = await _classRepository.GetClassTAAsync(classId, taId);
            if (classTA == null)
            {
                throw new Exception("Không tìm thấy trợ giảng trong lớp học này.");
            }
            if (classTA.Status == "Deactive")
            {
                throw new Exception("Trợ giảng này đã không còn trong lớp.");
            }
            classTA.Status = "Deactive";
            classTA.UpdatedAt = DateTime.UtcNow;
            await _classRepository.UpdateClassTAAsync(classTA);
            try
            {
                var classObj = await _classRepository.GetByIdAsync(classId);
                string className = classObj?.ClassName ?? "một lớp học";

                await _notificationService.SendNotificationAsync(
                    targetAccountId: taId,
                    studentId: null,
                    title: "Ngừng phân công",
                    content: $"Bạn đã ngừng công việc trợ giảng tại lớp '{className}'.",
                    actionUrl: "/assisted-classes", 
                    type: "Class"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi gửi thông báo gỡ TA khỏi lớp: {ex.Message}");
            }
            return true;
        }

        public async Task UpdateTaskStatusAsync(Guid taskId, UpdateTaskStatusDto newStatus)
        {
            var taId = _currentUser.UserId;
            var task = await _taRepository.GetTaskByIdAsync(taskId);
            if (task == null) throw new Exception("Nhiệm vụ không tồn tại!");
            if(task.ClassTa.Taid != taId) throw new Exception("Bạn không có quyền thao tác nhiệu vụ này!");

            if (newStatus.Status == "Done")
                throw new Exception("Chỉ giáo viên mới có quyền chuyển trạng thái sang Hoàn thành.");

            if (task.Status == "Done")
                throw new Exception("Nhiệm vụ đã hoàn thành và đóng lại, không thể thay đổi trạng thái.");

            if (task.Status == "Review" || task.Status == "In Review")
            {
                throw new Exception("Nhiệm vụ đang trong quá trình chờ giáo viên duyệt, bạn không thể thay đổi trạng thái lúc này.");
            }

            if (task.Status == newStatus.Status) return;

            task.Status = newStatus.Status;
            task.UpdatedAt = DateTime.UtcNow;
            await _taRepository.UpdateTaskAsync(task);

            //Notification
            if (newStatus.Status == "Review")
            {
                try
                {
                    var teacherAccountId = task.ClassTa.Class.TeacherId;
                    var className = task.ClassTa.Class.ClassName;

                    await _notificationService.SendNotificationAsync(
                        targetAccountId: teacherAccountId,
                        studentId: null,
                        title: "Nhiệm vụ chờ duyệt",
                        content: $"Trợ giảng đã hoàn thành nhiệm vụ: '{task.Title}' trong lớp {className}. Vui lòng kiểm tra và duyệt.",
                        actionUrl: $"/teacher/classes/{task.ClassTa.ClassId}/assistants",
                        type: "Task"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi gửi thông báo Review Task: {ex.Message}");
                }
            }
        }

        public async Task ReviewTaskAsync(Guid taskId, bool isApproved, string? teacherFeedback)
        {
            var userId = _currentUser.UserId;
            var task = await _taRepository.GetTaskByIdAsync(taskId);
            if (task == null) throw new Exception("Nhiệm vụ không tồn tại!");

            if (task.ClassTa.Class.TeacherId != userId) {
                throw new Exception("Bạn không có quyền thao tác nhiệm vụ này");
            } 
            
            if (task.Status != "Review")
            {
                throw new Exception($"Không thể thực hiện thao tác này. Nhiệm vụ hiện đang ở trạng thái: {task.Status}");
            }

            if (isApproved)
            {
                task.Status = "Done";
                task.Feedback = teacherFeedback; 
            }
            else
            {
                task.Status = "InProgress"; 
                task.Feedback = teacherFeedback; 
            }

            task.UpdatedAt = DateTime.UtcNow;
            await _taRepository.UpdateTaskAsync(task);

            try
            {
                await _notificationService.SendNotificationAsync(
                    targetAccountId: task.ClassTa.Taid,
                    studentId: null,
                    title: isApproved ? "Nhiệm vụ hoàn tất" : "Nhiệm vụ cần sửa lại",
                    content: $"Nhiệm vụ '{task.Title}' {(isApproved ? "đã được duyệt" : "cần được chỉnh sửa")}. Phản hồi: {teacherFeedback}",
                    actionUrl: $"/ta/tasks",
                    type: "Task"
                );
            }
            catch (Exception ex) { _logger.LogError(ex.Message); }
        }
    }
}
