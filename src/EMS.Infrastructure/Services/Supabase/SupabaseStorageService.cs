using EMS.Application.Common.Interfaces;
using EMS.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Services.Supabase
{
    public class SupabaseStorageService : ISupabaseStorageService
    {
        private readonly global::Supabase.Client _client;
        private readonly SupabaseSettings _settings;

        public SupabaseStorageService(global::Supabase.Client client, IOptions<SupabaseSettings> settings)
        {
            _client = client;
            _settings = settings.Value;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folderPath)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty.");

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var supabasePath = $"{folderPath}/{fileName}";

            var bucket = _client.Storage.From(_settings.BucketName);
            
            await bucket.Upload(fileBytes, supabasePath, new global::Supabase.Storage.FileOptions { CacheControl = "3600", Upsert = false });

            return bucket.GetPublicUrl(supabasePath);
        }

        public async Task DeleteFileByUrlAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;


            var publicPrefix = $"{_settings.Url}/storage/v1/object/public/{_settings.BucketName}/";
            if (fileUrl.StartsWith(publicPrefix))
            {
                var filePath = fileUrl.Substring(publicPrefix.Length);
                var bucket = _client.Storage.From(_settings.BucketName);
                await bucket.Remove(new System.Collections.Generic.List<string> { filePath });
            }
        }
    }
}
