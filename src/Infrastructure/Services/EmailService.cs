using System.Net;
using System.Net.Mail;
using AutoGallerySaaS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutoGallerySaaS.Infrastructure.Services;

/// <summary>
/// SMTP yapılandırılmışsa (Email:Host doluysa) gerçek e-posta gönderir;
/// yapılandırılmamışsa (geliştirme) e-postayı loga yazar. Böylece mail hesabı
/// hazır olmadan akış test edilebilir; hesap bağlanınca tek ayar değişikliğiyle gerçek gönderime geçer.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    // Mail (SMTP) yapılandırıldıysa true. Mail bağımlı özellikler (şifre sıfırlama vb.) buna göre aktifleşir.
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Email:Host"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Email:From"] ?? _configuration["Email:Username"]);

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var host = _configuration["Email:Host"];
        var fromAddress = _configuration["Email:From"] ?? _configuration["Email:Username"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
        {
            // Geliştirme modu: gerçek gönderim yok, içerik loga yazılır.
            _logger.LogInformation(
                "[DEV-EMAIL] (SMTP yapılandırılmadı) Alıcı: {To} | Konu: {Subject} | İçerik: {Body}",
                toEmail, subject, htmlBody);
            return;
        }

        var port = int.TryParse(_configuration["Email:Port"], out var parsedPort) ? parsedPort : 587;
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var enableSsl = !bool.TryParse(_configuration["Email:EnableSsl"], out var ssl) || ssl;

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, _configuration["Email:FromName"] ?? "AutoGallery"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = string.IsNullOrWhiteSpace(username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(username, password)
        };

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            // Mail gönderimi başarısız olsa bile akış (örn. giriş) çökmemeli; loglanır.
            _logger.LogError(ex, "E-posta gönderilemedi. Alıcı: {To}", toEmail);
        }
    }
}
