using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace Projeto_SEGUES.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            
            string mailServer = "smtp.gmail.com";
            int port = 587;
            string myEmail = "segues2026@gmail.com"; 
            string myPassword = "vuih xnzi kalq yivw"; 

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