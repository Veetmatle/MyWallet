using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using MailKit.Security; 

namespace MyWallet.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    // Konstruktor używa IOptions<EmailSettings>
    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentBytes, string attachmentName)
    {
        // Sprawdź czy ustawienia są poprawne
        if (_emailSettings == null)
            throw new InvalidOperationException("EmailSettings nie zostały skonfigurowane");
        
        if (string.IsNullOrEmpty(_emailSettings.SmtpHost))
            throw new InvalidOperationException("SmtpHost nie został skonfigurowany");

        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            TextBody = body
        };

        // Dodaj załącznik PDF
        if (attachmentBytes != null && attachmentBytes.Length > 0)
        {
            builder.Attachments.Add(attachmentName, attachmentBytes, new ContentType("application", "pdf"));
        }

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();

        try
        {
            // Połącz i zaloguj się do SMTP
            await smtp.ConnectAsync(
                _emailSettings.SmtpHost,
                _emailSettings.SmtpPort,
                _emailSettings.SmtpPort == 587 ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect);

            if (!string.IsNullOrWhiteSpace(_emailSettings.SmtpUser))
            {
                await smtp.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
            }

            await smtp.SendAsync(message);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Błąd wysyłania e-maila: {ex.Message}", ex);
        }
        finally
        {
            await smtp.DisconnectAsync(true);
        }
    }
}