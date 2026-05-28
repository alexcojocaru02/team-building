using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace TeamConnect.Api.Shared.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Skipping email send because recipient address is empty.");
                return false;
            }

            if (!IsEnabled())
            {
                _logger.LogInformation("Email delivery is disabled. Skipping message to {Recipient} with subject {Subject}.", toEmail, subject);
                return false;
            }

            var host = _configuration["Email:Smtp:Host"] ?? _configuration["Email:SmtpHost"];
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new InvalidOperationException("Email SMTP host is not configured.");
            }

            var fromAddress = _configuration["Email:FromAddress"] ?? _configuration["Email:Smtp:FromAddress"];
            if (string.IsNullOrWhiteSpace(fromAddress))
            {
                throw new InvalidOperationException("Email from address is not configured.");
            }

            var fromName = _configuration["Email:FromName"] ?? _configuration["Email:Smtp:FromName"] ?? "TeamConnect";
            var port = _configuration.GetValue<int?>("Email:Smtp:Port") ?? 587;
            var enableSsl = _configuration.GetValue<bool?>("Email:Smtp:EnableSsl") ?? true;
            var username = _configuration["Email:Smtp:Username"];
            var password = _configuration["Email:Smtp:Password"];

            using var message = new MailMessage();
            message.From = new MailAddress(fromAddress, fromName);
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;
            message.SubjectEncoding = Encoding.UTF8;
            message.BodyEncoding = Encoding.UTF8;

            if (!string.IsNullOrWhiteSpace(textBody))
            {
                var textView = AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, MediaTypeNames.Text.Plain);
                message.AlternateViews.Add(textView);
            }

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(username, password ?? string.Empty);
            }

            try
            {
                await client.SendMailAsync(message);
                _logger.LogInformation("Sent email to {Recipient} with subject {Subject}.", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient} with subject {Subject}.", toEmail, subject);
                return false;
            }
        }

        private bool IsEnabled()
        {
            var rawValue = _configuration["Email:Enabled"];
            return bool.TryParse(rawValue, out var enabled) && enabled;
        }
    }
}