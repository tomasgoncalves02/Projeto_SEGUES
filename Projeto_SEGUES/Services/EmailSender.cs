using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace Projeto_SEGUES.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        
        public EmailSender(IConfiguration config)
        {
            _config = config;
        }
        
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var mailServer = _config["EmailSettings:SmtpServer"];
            var port = int.Parse(_config["EmailSettings:SmtpPort"]!);
            var myEmail = _config["EmailSettings:SenderEmail"]!;
            var myPassword = _config["EmailSettings:SenderPassword"];
            
            var client = new SmtpClient(mailServer, port)
            {
                Credentials = new NetworkCredential(myEmail, myPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage(myEmail, email, subject, htmlMessage)
            {
                IsBodyHtml = true
            };

            return client.SendMailAsync(mailMessage);
        }
    }
}