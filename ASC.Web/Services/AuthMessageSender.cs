using ASC.Web.Configuration;
using MailKit.Net.Smtp;
<<<<<<< HEAD
using MailKit.Security;
=======
>>>>>>> 8da259071b53eaf611f1701a7493e18be3d08c90
using Microsoft.Extensions.Options;
using MimeKit;

namespace ASC.Web.Services
{
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
<<<<<<< HEAD
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
=======
        private IOptions<ApplicationSettings> _settings;

        public AuthMessageSender(IOptions<ApplicationSettings> settings)
        {
            _settings = settings;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("admin", _settings.Value.SMTPAccount));
            emailMessage.To.Add(new MailboxAddress("user", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("plain") { Text = message };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_settings.Value.SMTPServer, _settings.Value.SMTPPort, false);
                await client.AuthenticateAsync(_settings.Value.SMTPAccount, _settings.Value.SMTPPassword);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
>>>>>>> 8da259071b53eaf611f1701a7493e18be3d08c90
        }

        public Task SendSmsAsync(string number, string message)
        {
            return Task.CompletedTask;
        }
    }
}