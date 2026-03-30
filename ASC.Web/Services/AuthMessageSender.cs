using ASC.Web.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ASC.Web.Services
{
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        private readonly ApplicationSettings _settings;

        public AuthMessageSender(IOptions<ApplicationSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("ASC", _settings.SMTPAccount));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;

            message.Body = new TextPart("html")
            {
                Text = htmlMessage
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SMTPServer, _settings.SMTPPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.SMTPAccount, _settings.SMTPPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public Task SendSmsAsync(string number, string message)
        {
            return Task.CompletedTask;
        }
    }
}