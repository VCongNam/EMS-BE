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

        //Validate password
        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;
            // Ít nhất 8 ký tự
            if (password.Length < 8)
                return false;
            // Có chữ thường
            bool hasLower = Regex.IsMatch(password, "[a-z]");
            // Có chữ hoa
            bool hasUpper = Regex.IsMatch(password, "[A-Z]");
            // Có số
            bool hasDigit = Regex.IsMatch(password, "[0-9]");
            // Có ký tự đặc biệt
            bool hasSpecial = Regex.IsMatch(password, "[^a-zA-Z0-9]");
            // Không chứa khoảng trắng
            bool hasWhiteSpace = Regex.IsMatch(password, @"\s");

            return hasLower && hasUpper && hasDigit && hasSpecial && !hasWhiteSpace;
        }

        //Remove Vietnamese Signs
        public static string RemoveVietnameseSigns(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;

            string[] VietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };

            for (int i = 1; i < VietnameseSigns.Length; i++)
            {
                for (int j = 0; j < VietnameseSigns[i].Length; j++)
                {
                    str = str.Replace(VietnameseSigns[i][j], VietnameseSigns[0][i - 1]);
                }
            }
            return str;
        }
    }
}
