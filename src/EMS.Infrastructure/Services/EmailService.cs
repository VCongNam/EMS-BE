using EMS.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace EMS.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration config;
        public EmailService(IConfiguration config)
        {
            this.config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            // 1. Truy xuất chính xác các Key từ file JSON của bạn
            var senderName = config["EmailSettings:SenderName"];
            var senderEmail = config["EmailSettings:SenderEmail"];
            var password = config["EmailSettings:Password"];
            var smtpServer = config["EmailSettings:SmtpServer"];

            // DEBUG: Hãy nhìn vào màn hình Console khi chạy API để kiểm tra dòng này
            Console.WriteLine($"--- DEBUG EMAIL ---");
            Console.WriteLine($"Sender: {senderEmail}");
            Console.WriteLine($"To: {toEmail}");

            // Kiểm tra nếu config bị null (thường do sai đường dẫn trong appsettings)
            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
            {
                throw new Exception("Lỗi: Không đọc được cấu hình Email từ appsettings.json. Kiểm tra lại tên Key!");
            }

            var email = new MimeMessage();

            // ĐẢM BẢO DÒNG NÀY ĐƯỢC CHẠY: Đây là lệnh gán "Người gửi"
            email.From.Add(new MailboxAddress(senderName, senderEmail));

            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };

            using var smtp = new SmtpClient();
            try
            {
                // Bỏ qua kiểm tra chứng chỉ SSL nếu gặp lỗi Revocation
                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await smtp.ConnectAsync(smtpServer, 587, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(senderEmail, password);
                await smtp.SendAsync(email);

                Console.WriteLine("=> Gửi Email thành công!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=> Lỗi SMTP: {ex.Message}");
                throw;
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}   
