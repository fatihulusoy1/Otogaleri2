namespace AutoGallerySaaS.Application.Common.Interfaces;

public interface IEmailService
{
    bool IsConfigured { get; }
    Task SendAsync(string toEmail, string subject, string htmlBody);
}
