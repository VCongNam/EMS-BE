using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using EMS.Application.Common.Interfaces;
using EMS.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Services
{
    public class ImageService : IImageService
    {
        private readonly Cloudinary cloudinary;


        public ImageService(IOptions<CloudinarySettings> config)
        {
            var account = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );
            cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadAvatarAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return string.Empty;
            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face"),
                Folder = "EMS_Avatars"
            };
            var result = await cloudinary.UploadAsync(uploadParams);
            return result.SecureUrl.ToString();
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return true;

            // Bóc tách PublicId (Ví dụ: EMS_Avatars/v123/abc.jpg -> EMS_Avatars/abc)
            var publicId = GetPublicIdFromUrl(imageUrl);
            if (string.IsNullOrEmpty(publicId)) return false;

            var deletionParams = new DeletionParams(publicId);
            var result = await cloudinary.DestroyAsync(deletionParams);
            return result.Result == "ok";
        }

        private string GetPublicIdFromUrl(string url)
        {
            try
            {
                // Cắt chuỗi để lấy phần sau 'upload/' và bỏ phần version (v1234567/)
                var parts = url.Split('/');
                var uploadIndex = Array.IndexOf(parts, "upload");

                // Lấy các phần sau 'vxxxx/' (thường là folder/name.ext)
                var publicIdWithExt = string.Join("/", parts.Skip(uploadIndex + 2));
                return publicIdWithExt.Split('.')[0]; // Bỏ đuôi .jpg, .png
            }
            catch { return string.Empty; }
        }
    }
}
