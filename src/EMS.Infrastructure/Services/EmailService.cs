using EMS.Application.Common.Interfaces;
using EMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EMS.Application.Common.Interfaces;

namespace EMS.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration config;
        private readonly HttpClient httpClient;

        public EmailService(IConfiguration config, HttpClient httpClient)
        {
            this.config = config;
            this.httpClient = httpClient;
        }

        public async Task SendEmailAsync(EmailMessage message)
        {
            var apiKey = config["Brevo:ApiKey"];
            var senderEmail = config["Brevo:SenderEmail"];
            var senderName = config["Brevo:SenderName"];

            // 1. Chuẩn bị dữ liệu gửi lên Brevo (JSON)
            var payload = new
            {
                sender = new { name = senderName, email = senderEmail },
                to = new[] { new { email = message.To } },
                subject = message.Subject,
                htmlContent = message.Body
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // 2. Cấu hình Header chứa API Key để xác thực
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
            httpClient.DefaultRequestHeaders.Add("accept", "application/json");

            try
            {
                // 3. Bắn HTTP POST thẳng tới máy chủ Brevo (Cổng 443 - không bị Render chặn)
                var response = await httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetail = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi từ máy chủ Brevo: {errorDetail}");
                }

                Console.WriteLine($"[EMAIL SUCCESS] Đã gửi email thành công tới: {message.To}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
                throw new Exception("Hệ thống gửi mail đang gặp sự cố. Vui lòng thử lại sau!");
            }
        }
    }
}
