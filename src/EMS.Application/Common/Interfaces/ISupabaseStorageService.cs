using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EMS.Application.Common.Interfaces
{
    public interface ISupabaseStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string folderPath);
        Task DeleteFileByUrlAsync(string fileUrl);
    }
}
