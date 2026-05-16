namespace IT15_MonticalboFinalProj.Services;

public interface IEmailService
{
    Task SendEmailAsync(string email, string subject, string message);
}
