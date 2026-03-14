using EMS.Application.Common.Interfaces;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MailKit.Net.Smtp;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

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
            var settings = config.GetSection("EmailSettings");
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(settings["SenderName"], settings["SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = htmlMessage
            };

            using var smtp = new SmtpClient();
            try {
                await smtp.ConnectAsync(settings["SmtpServer"], int.Parse(settings["Port"]!), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(settings["SenderEmail"], settings["Password"]);
                await smtp.SendAsync(email);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}   
