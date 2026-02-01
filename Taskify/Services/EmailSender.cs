using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace Taskify.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly string? _myEmail;
        private readonly string? _myPassword;

        public EmailSender(IConfiguration configuration)
        {
            _myEmail = configuration.GetSection("EmailSettings").GetSection("Email").Value;
            _myPassword = configuration.GetSection("EmailSettings").GetSection("Password").Value;
        }

        public Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials =  false,
                Credentials = new NetworkCredential(_myEmail, _myPassword),
            };

            var mailMessage = new MailMessage()
            {
                From = new MailAddress(_myEmail ?? throw new InvalidOperationException(), "Taskify App"),
                Subject = subject,
                Body =  htmlMessage,
                IsBodyHtml = true
            };
        
            mailMessage.To.Add(toEmail);
        
            return client.SendMailAsync(mailMessage);
        }
    }
}