using EMS.Application.Common.DTOs;
using EMS.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Services
{
    public class VietQRService : IVietQRService
    {
        private readonly HttpClient _httpClient;
        public VietQRService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.vietqr.io/");
        }

        public async Task<string> GenerateQRCodeAsync(VietQRRequest request)
        {
            var payload = new
            {
                accountNo = request.AccountNo,
                accountName = request.AccountName,
                acqId = request.BankId,
                amount = request.Amount,
                addInfo = request.Content,
                format = "text",
                template = "compact"
            };

            var response = await _httpClient.PostAsJsonAsync("v2/generate", payload);
                if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Lỗi khi kết nối đến cổng tạo QR Code VietQR.");
            }
            var responseString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseString);
            var qrDataUrl = jsonDoc.RootElement
                .GetProperty("data")
                .GetProperty("qrDataURL")
                .GetString();

            return qrDataUrl ?? string.Empty;
        }
    }
}
