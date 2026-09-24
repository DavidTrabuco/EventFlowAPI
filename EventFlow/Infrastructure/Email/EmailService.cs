using EventFlow.Domain.Interface;
using EventFlow.Domain.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EventFlow.Infrastructure.Email
{
    public class EmailService : IEmailService
    {
        private readonly EmailOptions _options;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task EnviarAsync(string destinatario, string assunto, string corpoHtml)
        {
            var mensagem = new MimeMessage();
            mensagem.From.Add(MailboxAddress.Parse(_options.From));
            mensagem.To.Add(MailboxAddress.Parse(destinatario));
            mensagem.Subject = assunto;
            mensagem.Body = new TextPart("html") { Text = corpoHtml };

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_options.From, _options.AppPassword);
                await smtp.SendAsync(mensagem);
            }
            finally
            {
                if (smtp.IsConnected)
                {
                    await smtp.DisconnectAsync(true);
                }
            }

            _logger.LogInformation("E-mail enviado para {Destinatario} com assunto '{Assunto}'.", destinatario, assunto);
        }
    }
}
