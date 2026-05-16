using System.Net;
using System.Net.Mail;
using IT15_MonticalboFinalProj.Models;
using Microsoft.Extensions.Options;

namespace IT15_MonticalboFinalProj.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        try
        {
            using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPass),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            // Log error or handle silently for now to not break the application flow
            Console.WriteLine($"Email failed: {ex.Message}");
        }
    }
}
