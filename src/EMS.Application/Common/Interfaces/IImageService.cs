using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EMS.Application.Common.Interfaces
{
    public interface IImageService
    {
        Task<string> UploadAvatarAsync(IFormFile file);
        Task<bool> DeleteImageAsync(string imageUrl);
    }
}
