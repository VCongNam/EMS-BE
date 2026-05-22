using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace EMS.Application.Features.LearningMaterials.Validators
{
    internal static class LearningMaterialFileValidation
    {
        public const long MaxFileSize = 10 * 1024 * 1024;

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

        public static IRuleBuilderOptions<T, IFormFile> ApplyFileRules<T>(this IRuleBuilder<T, IFormFile> ruleBuilder)
        {
            return ruleBuilder
                .Must(file => file.Length <= MaxFileSize)
                .WithMessage((_, file) => $"File '{file.FileName}' vượt quá dung lượng tối đa 10MB.")
                .Must(file => file.ContentType.StartsWith("image/") || AllowedMimeTypes.Contains(file.ContentType))
                .WithMessage((_, file) => $"Định dạng file '{file.ContentType}' không được hỗ trợ.");
        }
    }
}
