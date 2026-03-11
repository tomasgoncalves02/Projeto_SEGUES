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
            var myPassword = _config["Secrets:SenderPassword"];
            
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
        
        public string GetEmailBody(string title, string name, string content)
        {
            return $$"""
                                     <!DOCTYPE html>
                                     <html>
                                     <head>
                                         <meta charset='utf-8'>
                                         <style>
                                             body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7f6; margin: 0; padding: 0; }
                                             .container { max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05); }
                                             .header { background-color: #009697; padding: 30px; text-align: center; color: #ffffff; }
                                             .header h1 { margin:0; font-size: 28px; color: #ffffff !important; }
                                             .content { padding: 40px; line-height: 1.6; color: #333333; }
                                             .button-container { text-align: center; margin: 30px 0; }
                                             .button { background-color: #009697; color: #ffffff !important; padding: 15px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block; }
                                             .footer { background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #777777; }
                                             .security-note { border-top: 1px solid #eeeeee; margin-top: 30px; padding-top: 20px; font-size: 13px; color: #999999; }
                                             .text-color-ips { color: #009697; }
                                         </style>
                                     </head>
                                     <body>
                                         <div class='container'>
                                             <div class='header'>
                                                 <h1 style='color: white;'>SEGUES</h1>
                                                 <p style='margin:0; opacity: 0.8; color: white;'>Controlo de Refeições</p>
                                             </div>
                                             <div class='content'>
                                                 <h2 class='text-color-ips'>{{title}}</h2>
                                                 <p>Olá, {{name}}!</p>
                                                 {{content}}
                                             </div>
                                             <div class='footer'>
                                                 &copy; 2026 SEGUES - Sistema de Gestão de Refeições.
                                             </div>
                                         </div>
                                     </body>
                                 </html>
                     """;
        }
    }
}