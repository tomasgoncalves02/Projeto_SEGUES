using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace Projeto_SEGUES.Services;

/// <summary>
/// Service responsible for handling email communications within the application.
/// Implements <see cref="IEmailSender"/> to provide SMTP-based email delivery.
/// </summary>
public class EmailSender : IEmailSender
{
    /// <summary>Application configuration for retrieving SMTP settings and credentials.</summary>
    private readonly IConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailSender"/> class.
    /// </summary>
    /// <param name="config">The system configuration interface.</param>
    public EmailSender(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Sends an email asynchronously using the configured SMTP server.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="subject">The subject line of the email.</param>
    /// <param name="htmlMessage">The body of the email in HTML format.</param>
    /// <returns>A task representing the asynchronous email delivery operation.</returns>
    /// <remarks>
    /// SMTP settings are retrieved from the "EmailSettings" section of the configuration, 
    /// while sensitive credentials should be stored in "Secrets".
    /// </remarks>
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var mailServer = _config["EmailSettings:SmtpServer"];
        var port = int.Parse(_config["EmailSettings:SmtpPort"]!);
        var myEmail = _config["EmailSettings:SenderEmail"]!;
        var myPassword = _config["Secrets:SenderPassword"];

        var client = new SmtpClient(mailServer, port)
        {
            Credentials = new NetworkCredential(myEmail, myPassword),
            EnableSsl = true
        };

        var mailMessage = new MailMessage(myEmail, email, subject, htmlMessage)
        {
            IsBodyHtml = true
        };

        return client.SendMailAsync(mailMessage);
    }

    /// <summary>
    /// Generates a standardized HTML email template with the application's branding.
    /// </summary>
    /// <param name="title">The heading title to appear inside the email body.</param>
    /// <param name="name">The name of the recipient for the greeting.</param>
    /// <param name="content">The specific message content or instructions.</param>
    /// <returns>A string containing the full HTML document for the email.</returns>
    /// <remarks>
    /// This template uses the institutional Teal color (#009697) and includes 
    /// a responsive container for consistent display across different mail clients.
    /// </remarks>
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