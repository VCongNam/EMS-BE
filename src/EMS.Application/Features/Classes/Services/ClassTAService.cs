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

        public ClassTAService(IClassRepository classRepository, ITARepository tARepository, INotificationService notificationService, ILogger<AssignmentService> logger)
        {
            _classRepository = classRepository;
            _taRepository = tARepository;
            _notificationService = notificationService;
            _logger = logger;
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
            var newTask = new TeachingAssistantTask
            {
                TataskId = Guid.NewGuid(),
                ClassTaid = request.ClassTAID,
                Title = request.Title,
                DueDate = request.DueDate,
                Status = "Pending",
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
                Type = t.Type ?? "N/A"
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

        // 1. Lấy tất cả Task của TA đó từ tất cả các lớp họ tham gia
        public async Task<IEnumerable<TaskDto>> GetTasksByTAIdAsync(Guid taId)
        {
            // Bạn cần viết thêm hàm này trong TARepository để query:
            // Join từ ClassTA sang TeachingAssistantTask dựa trên TAID
            var tasks = await _taRepository.GetTasksByTAIdAsync(taId);

            return tasks.Select(t => new TaskDto
            {
                TATaskID = t.TataskId,
                Title = t.Title,
                DueDate = t.DueDate,
                Status = t.Status ?? "N/A",
                Type = t.Type ?? "N/A"
            }).ToList();
        }

        // 2. Lấy danh sách lớp học mà TA này được phân công
        public async Task<IEnumerable<AssignedClassDto>> GetClassesByTAIdAsync(Guid taId)
        {
            // Query bảng ClassTA (ClassTum) lọc theo TAID và Include bảng Class
            var assignments = await _classRepository.GetClassesByTAIdAsync(taId);

            return assignments.Select(a => new AssignedClassDto
            {
                ClassID = a.ClassId,
                ClassName = a.Class.ClassName, // Giả sử bảng Class có trường ClassName
                Permission = a.Permission,
                SalaryPerSession = a.SalaryPerSession
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
    }
}
