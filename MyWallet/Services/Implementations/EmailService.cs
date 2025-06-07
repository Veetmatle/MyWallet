using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MyWallet.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailWithAttachmentAsync(
            string toEmail,
            string subject,
            string body,
            byte[] attachmentBytes,
            string attachmentName)
        {
            if (_emailSettings == null)
                throw new InvalidOperationException("EmailSettings nie zostały skonfigurowane");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { TextBody = body };

            if (attachmentBytes?.Length > 0)
            {
                // MimeKit dobierze MIME-type na podstawie rozszerzenia pliku
                builder.Attachments.Add(attachmentName, attachmentBytes);
            }

            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtp.ConnectAsync(
                _emailSettings.SmtpHost,
                _emailSettings.SmtpPort,
                _emailSettings.SmtpPort == 587 ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect);

            if (!string.IsNullOrWhiteSpace(_emailSettings.SmtpUser))
                await smtp.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);

            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
