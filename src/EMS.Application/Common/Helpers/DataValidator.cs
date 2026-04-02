using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EMS.Application.Common.Helpers
{
    public static class DataValidator
    {
        //Validate File
        public static void ValidateFile(IFormFile file)
        {
            int maxSizeMegabytes = 10;
            long maxBytes = maxSizeMegabytes * 1024 * 1024;
            if (file.Length > maxBytes)
                throw new Exception($"File '{file.FileName}' vượt quá dung lượng tối đa {maxSizeMegabytes}MB.");

            var ext = Path.GetExtension(file.FileName).ToLower();
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".zip", ".rar" };

            if (!allowedExtensions.Contains(ext))
                throw new Exception($"Định dạng file '{ext}' không được hỗ trợ.");
        }

        //Validate Phone Number
        public static bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            // 2 form 0xxxx và 84/+84xxx
            string pattern = @"^(0|\+84|84)(3|5|7|8|9)[0-9]{8}$";
            return Regex.IsMatch(phone, pattern);
        }

        //Validate Email
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
