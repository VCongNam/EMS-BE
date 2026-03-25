using EMS.Application.Common.Interfaces;
using EMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Json; // Cực kỳ quan trọng để dùng PostAsJsonAsync
using System.Threading.Tasks;

namespace EMS.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public EmailService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public async Task SendEmailAsync(EmailMessage message)
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];

            // 1. Gói dữ liệu (Payload) theo chuẩn Brevo
            var payload = new
            {
                sender = new { name = senderName, email = senderEmail },
                to = new[] { new { email = message.To } },
                subject = message.Subject,
                htmlContent = message.Body
            };

            // 2. Gắn API Key vào Header
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

            try
            {
                // 3. Gửi thẳng Payload dưới dạng JSON (Tự động xử lý JsonSerializer cho bạn)
                var response = await _httpClient.PostAsJsonAsync("https://api.brevo.com/v3/smtp/email", payload);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetail = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi Brevo: {errorDetail}");
                }

                Console.WriteLine($"[EMAIL SUCCESS] Đã gửi thư tới: {message.To}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL EXCEPTION] {ex.Message}");
                throw new Exception("Hệ thống gửi mail gặp sự cố.");
            }
        }
    }
}
