using EMS.Application.Common.Interfaces;
using EMS.Application.Features.LearningMaterials.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Application.Features.LearningMaterials.Services
{
    public class LearningMaterialService : ILearningMaterialService
    {
        private readonly ILearningMaterialRepository _materialRepository;
        private readonly ISupabaseStorageService _storageService;
        private readonly ICurrentUserService _currentUserService;

        // Giới hạn file: 10MB
        private const long MaxFileSize = 10 * 1024 * 1024;
        private static readonly string[] AllowedMimeTypes =
        {
            "image/png", "image/jpeg", "image/jpg", "image/gif", "image/webp", "image/svg+xml", "image/bmp",
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/zip",
            "application/x-rar-compressed"
        };

        public LearningMaterialService(
            ILearningMaterialRepository materialRepository,
            ISupabaseStorageService storageService,
            ICurrentUserService currentUserService)
        {
            _materialRepository = materialRepository;
            _storageService = storageService;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> CreateLearningMaterialAsync(CreateLearningMaterialDto request)
        {
            var materialId = Guid.NewGuid();

            var material = new LearningMaterial
            {
                MaterialId = materialId,
                ClassId = request.ClassId,
                AuthorId = _currentUserService.UserId,
                Title = request.Title,
                Description = request.Description,
                // Đã loại bỏ FileUrl ở đây
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await _materialRepository.AddAsync(material);

            // Chỉ xử lý danh sách Attachments nếu có
            if (request.Attachments != null && request.Attachments.Count > 0)
            {
                foreach (var file in request.Attachments)
                {
                    ValidateFile(file.FileName, file.Length, file.ContentType);

                    var attachmentUrl = await _storageService.UploadFileAsync(file, $"materials/{materialId}/attachments");

                    var attachment = new MaterialAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        MaterialId = materialId,
                        FileName = file.FileName,
                        FileUrl = attachmentUrl,
                        FileType = file.ContentType,
                        FileSize = file.Length,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _materialRepository.AddAttachmentAsync(attachment);
                }
            }

            return material.MaterialId;
        }

        public async Task UpdateLearningMaterialAsync(Guid id, UpdateLearningMaterialDto request)
        {
            var material = await _materialRepository.GetByIdAsync(id);
            if (material == null)
                throw new Exception($"Learning material with ID {id} not found.");

            material.Title = request.Title;
            material.Description = request.Description;
            material.UpdatedAt = DateTime.UtcNow;

            // Không còn logic cập nhật FileUrl chính ở đây

            await _materialRepository.UpdateAsync(material);

            // 1. Xóa các attachments cũ theo yêu cầu
            if (request.RemoveAttachmentIds != null && request.RemoveAttachmentIds.Count > 0)
            {
                foreach (var attachmentId in request.RemoveAttachmentIds)
                {
                    var attachment = await _materialRepository.GetAttachmentByIdAsync(attachmentId);
                    if (attachment != null)
                    {
                        await _storageService.DeleteFileByUrlAsync(attachment.FileUrl);
                        await _materialRepository.RemoveAttachmentAsync(attachment);
                    }
                }
            }

            // 2. Thêm các attachments mới nếu có
            if (request.NewAttachments != null && request.NewAttachments.Count > 0)
            {
                foreach (var file in request.NewAttachments)
                {
                    ValidateFile(file.FileName, file.Length, file.ContentType);

                    var attachmentUrl = await _storageService.UploadFileAsync(file, $"materials/{id}/attachments");

                    var attachment = new MaterialAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        MaterialId = id,
                        FileName = file.FileName,
                        FileUrl = attachmentUrl,
                        FileType = file.ContentType,
                        FileSize = file.Length,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _materialRepository.AddAttachmentAsync(attachment);
                }
            }
        }

        public async Task DeleteLearningMaterialAsync(Guid id)
        {
            var material = await _materialRepository.GetByIdAsync(id);
            if (material == null) throw new Exception("Learning material not found.");

            material.IsDeleted = true;
            material.UpdatedAt = DateTime.UtcNow;

            await _materialRepository.UpdateAsync(material);
        }

        public async Task<LearningMaterialResponseDto> GetLearningMaterialDetailAsync(Guid materialId)
        {
            var material = await _materialRepository.GetByIdWithDetailsAsync(materialId);
            if (material == null)
                throw new Exception("Learning material not found or has been deleted.");

            return new LearningMaterialResponseDto
            {
                MaterialId = material.MaterialId,
                ClassId = material.ClassId,
                AuthorName = material.Author?.FullName ?? "Unknown",
                Title = material.Title,
                Description = material.Description,
                // FileUrl đã bị loại bỏ khỏi ResponseDto
                CreatedAt = material.CreatedAt,
                UpdatedAt = material.UpdatedAt,
                Attachments = material.MaterialAttachments.Select(a => new MaterialAttachmentDto
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                    CreatedAt = a.CreatedAt
                }).ToList()
            };
        }

        public async Task<IEnumerable<LearningMaterialSummaryDto>> GetLearningMaterialsByClassIdAsync(Guid classId)
        {
            var materials = await _materialRepository.GetByClassIdAsync(classId);

            return materials.Select(m => new LearningMaterialSummaryDto
            {
                MaterialId = m.MaterialId,
                Title = m.Title,
                Description = m.Description,
                AuthorName = m.Author?.FullName ?? "Unknown",
                CreatedAt = m.CreatedAt
            });
        }

        private void ValidateFile(string fileName, long fileSize, string contentType)
        {
            if (fileSize > MaxFileSize)
                throw new Exception($"File '{fileName}' exceeds maximum size of 10MB.");

            if (contentType.StartsWith("image/")) return;

            if (!AllowedMimeTypes.Contains(contentType))
                throw new Exception($"File type '{contentType}' is not allowed.");
        }
    }
}