using EMS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Services
{
    public class CaptchaService : ICaptchaService
    {
        private readonly string secretKey;
        private readonly HttpClient httpClient;

        public CaptchaService(IConfiguration configuration, HttpClient httpClient)
        {
            this.secretKey = configuration["GoogleReCaptcha:SecretKey"] ?? throw new ArgumentNullException("Thiếu cấu hình GoogleReCaptcha:SecretKey");
            this.httpClient = httpClient;
        }

        public async Task<bool> VerifyCaptchaAsync(string token)
        {
            var response = await httpClient.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
                null);

            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);

            return doc.RootElement.GetProperty("success").GetBoolean();

        }
    }
}
