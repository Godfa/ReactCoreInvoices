using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _appUrl;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
            _smtpHost = _config["Email:SmtpHost"];
            _smtpPort = int.TryParse(_config["Email:SmtpPort"], out var port) ? port : 587;
            _smtpUsername = _config["Email:SmtpUsername"];
            _smtpPassword = _config["Email:SmtpPassword"];
            _fromEmail = _config["Email:FromEmail"];
            _fromName = _config["Email:FromName"] ?? "ReactCore Invoices";
            _appUrl = _config["Email:AppUrl"] ?? "https://your-app-url.com";
        }

        public async Task<bool> SendNewUserEmailAsync(string email, string displayName, string username, string temporaryPassword)
        {
            var subject = "Welcome to ReactCore Invoices - Your Account Details";
            var htmlContent = $@"
                <h2>Welcome to ReactCore Invoices!</h2>
                <p>Hi {displayName},</p>
                <p>Your account has been created successfully. Here are your login credentials:</p>
                <ul>
                    <li><strong>Username:</strong> {username}</li>
                    <li><strong>Email:</strong> {email}</li>
                    <li><strong>Temporary Password:</strong> {temporaryPassword}</li>
                </ul>
                <p><strong>Important:</strong> You will be required to change your password upon first login for security reasons.</p>
                <p>Please log in at: <a href=""{_appUrl}"">{_appUrl}</a></p>
                <br>
                <p>Best regards,<br>ReactCore Invoices Team</p>
            ";

            var plainTextContent = $@"
Welcome to ReactCore Invoices!

Hi {displayName},

Your account has been created successfully. Here are your login credentials:

Username: {username}
Email: {email}
Temporary Password: {temporaryPassword}

Important: You will be required to change your password upon first login for security reasons.

Please log in at: {_appUrl}

Best regards,
ReactCore Invoices Team
            ";

            return await SendEmailAsync(email, displayName, subject, plainTextContent, htmlContent);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string displayName, string resetLink)
        {
            var subject = "Password Reset Required - ReactCore Invoices";
            var htmlContent = $@"
                <h2>Password Reset Required</h2>
                <p>Hi {displayName},</p>
                <p>Your administrator has requested a password reset for your account.</p>
                <p>You will be required to change your password upon your next login.</p>
                <p>Please log in at: <a href=""{_appUrl}"">{_appUrl}</a></p>
                <p>If you did not request this change, please contact your administrator immediately.</p>
                <br>
                <p>Best regards,<br>ReactCore Invoices Team</p>
            ";

            var plainTextContent = $@"
Password Reset Required

Hi {displayName},

Your administrator has requested a password reset for your account.

You will be required to change your password upon your next login.

Please log in at: {_appUrl}

If you did not request this change, please contact your administrator immediately.

Best regards,
ReactCore Invoices Team
            ";

            return await SendEmailAsync(email, displayName, subject, plainTextContent, htmlContent);
        }

        private async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string plainTextContent, string htmlContent)
        {
            if (string.IsNullOrEmpty(_smtpHost))
            {
                _logger.LogWarning("SMTP host is not configured. Email will not be sent to {Email}", toEmail);
                return false;
            }

            if (string.IsNullOrEmpty(_fromEmail))
            {
                _logger.LogWarning("FromEmail is not configured. Email will not be sent to {Email}", toEmail);
                return false;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_fromName, _fromEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    TextBody = plainTextContent,
                    HtmlBody = htmlContent
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);

                // Authenticate if credentials are provided
                if (!string.IsNullOrEmpty(_smtpUsername) && !string.IsNullOrEmpty(_smtpPassword))
                {
                    await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending email to {Email}", toEmail);
                return false;
            }
        }
    }
}
