using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Students.DTOs;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.Services
{
    public class StudentMaterialService : IStudentMaterialService
    {
        private readonly ILearningMaterialRepository _materialRepo;
        private readonly IClassRepository _classRepo;
        private readonly ICurrentUserService _currentUserService;

        public StudentMaterialService(ILearningMaterialRepository materialRepo, IClassRepository classRepo, ICurrentUserService currentUserService)
        {
            _materialRepo = materialRepo;
            _classRepo = classRepo;
            _currentUserService = currentUserService;
        }

        public async Task<List<MaterialDto>> GetClassMaterialsAsync(Guid classId)
        {
            Guid studentId = _currentUserService.UserId;

            bool isEnrolled = await _classRepo.IsStudentAlreadyEnrolledAsync(classId, studentId);
            if (!isEnrolled)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem tài liệu lớp này!");
            }

            var materials = await _materialRepo.GetByClassIdAsync(classId);

            var result = materials.Select(m => new MaterialDto
            {
                MaterialID = m.MaterialId,
                Title = m.Title,
                Description = m.Description,
                CreatedAt = (DateTime)m.CreatedAt,
                Attachments = m.MaterialAttachments.Select(a => new MaterialAttachmentDto
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                }).ToList(),
            }).ToList();
            return result;
        }
    }
}
