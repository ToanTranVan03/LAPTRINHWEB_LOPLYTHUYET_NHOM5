using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace TechShare.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var enabled = _configuration.GetValue<bool>("EmailSettings:Enabled");
        if (!enabled)
        {
            _logger.LogInformation("Email sending is disabled. Skip mail to {Email}. Subject: {Subject}", email, subject);
            return;
        }

        var host = _configuration["EmailSettings:Host"];
        var port = _configuration.GetValue<int?>("EmailSettings:Port") ?? 587;
        var username = _configuration["EmailSettings:Username"];
        var password = _configuration["EmailSettings:Password"];
        var fromEmail = _configuration["EmailSettings:FromEmail"] ?? username;
        var fromName = _configuration["EmailSettings:FromName"] ?? "TechShare System";
        var useSsl = _configuration.GetValue<bool?>("EmailSettings:UseSsl") ?? true;

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogWarning("Email settings are incomplete. Skip sending mail to {Email}.", email);
            return;
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        mail.To.Add(email);

        using var smtp = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = useSsl
        };

        await smtp.SendMailAsync(mail);
    }
}
