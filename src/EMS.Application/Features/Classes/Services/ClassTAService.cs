using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Classes.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
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

        public ClassTAService(IClassRepository classRepository, ITARepository tARepository)
        {
            _classRepository = classRepository;
            _taRepository = tARepository;
        }
        public async Task<Guid> AssignTAAsync(Guid classId, AssignTADto request)
        {
            bool isAssigned = await _classRepository.IsTAAssignedAsync(classId, request.TAID);
            if (isAssigned)
            {
                throw new Exception("Trợ giảng đã được phân công vào lớp này rồi!");
            }

            var newClassTA = new ClassTum
            {
                ClassTaid = Guid.NewGuid(),
                ClassId = classId,
                Taid = request.TAID,
                Permission = request.Permission,
                SalaryPerSession = request.SalaryPerSession,
                CreatedAt = DateTime.Now,
            };

            await _classRepository.AddClassTAAsync(newClassTA);
            return newClassTA.ClassTaid;
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
            }).ToList();
        }

        public async Task UpdateTAPermissionAsync(Guid classId, Guid taId, UpdateTAPermissionDto request)
        {
            var classTa = await _classRepository.GetClassTAAsync(classId, taId);
            if(classTa == null)
            {
                throw new Exception("Không tìm thấy trợ giảng này trong lớp.");
            }

            classTa.Permission = request.Permission;
            classTa.UpdatedAt = DateTime.Now;

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
    }
}
