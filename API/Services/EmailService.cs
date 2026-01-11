using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
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
            _fromName = _config["Email:FromName"] ?? "Mökkilan Invoices";
            _appUrl = _config["Email:AppUrl"] ?? "https://your-app-url.com";
        }

        public async Task<bool> SendNewUserEmailAsync(string email, string displayName, string username, string temporaryPassword)
        {
            var subject = "Welcome to Mökkilan Invoices - Your Account Details";
            var htmlContent = $@"
                <h2>Welcome to Mökkilan Invoices!</h2>
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
                <p>Best regards,<br>Mökkilan Invoices Team</p>
            ";

            var plainTextContent = $@"
Welcome to Mökkilan Invoices!

Hi {displayName},

Your account has been created successfully. Here are your login credentials:

Username: {username}
Email: {email}
Temporary Password: {temporaryPassword}

Important: You will be required to change your password upon first login for security reasons.

Please log in at: {_appUrl}

Best regards,
Mökkilan Invoices Team
            ";

            return await SendEmailAsync(email, displayName, subject, plainTextContent, htmlContent);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string displayName, string temporaryPassword)
        {
            var subject = "Password Reset - Mökkilan Invoices";
            var htmlContent = $@"
                <h2>Password Reset</h2>
                <p>Hi {displayName},</p>
                <p>Your administrator has reset your password. Here is your new temporary password:</p>
                <p><strong>Temporary Password:</strong> {temporaryPassword}</p>
                <p><strong>Important:</strong> You will be required to change this password upon your next login for security reasons.</p>
                <p>Please log in at: <a href=""{_appUrl}"">{_appUrl}</a></p>
                <p>If you did not request this change, please contact your administrator immediately.</p>
                <br>
                <p>Best regards,<br>Mökkilan Invoices Team</p>
            ";

            var plainTextContent = $@"
Password Reset

Hi {displayName},

Your administrator has reset your password. Here is your new temporary password:

Temporary Password: {temporaryPassword}

Important: You will be required to change this password upon your next login for security reasons.

Please log in at: {_appUrl}

If you did not request this change, please contact your administrator immediately.

Best regards,
Mökkilan Invoices Team
            ";

            return await SendEmailAsync(email, displayName, subject, plainTextContent, htmlContent);
        }

        public async Task<bool> SendPasswordResetLinkAsync(string email, string displayName, string resetLink)
        {
            var subject = "Password Reset Request - Mökkilan Invoices";
            var htmlContent = $@"
                <h2>Password Reset Request</h2>
                <p>Hi {displayName},</p>
                <p>We received a request to reset your password. Click the link below to reset your password:</p>
                <p><a href=""{resetLink}"" style=""display: inline-block; padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px;"">Reset Password</a></p>
                <p>Or copy and paste this link into your browser:</p>
                <p>{resetLink}</p>
                <p><strong>This link will expire in 24 hours.</strong></p>
                <p>If you did not request a password reset, please ignore this email or contact support if you have concerns.</p>
                <br>
                <p>Best regards,<br>Mökkilan Invoices Team</p>
            ";

            var plainTextContent = $@"
Password Reset Request

Hi {displayName},

We received a request to reset your password. Click the link below to reset your password:

{resetLink}

This link will expire in 24 hours.

If you did not request a password reset, please ignore this email or contact support if you have concerns.

Best regards,
Mökkilan Invoices Team
            ";

            return await SendEmailAsync(email, displayName, subject, plainTextContent, htmlContent);
        }

        public async Task<bool> SendInvoiceReviewNotificationAsync(string email, string displayName, string invoiceTitle, string invoiceUrl)
        {
            var subject = "Tarkista lasku - Mökkilan Invoices";
            var htmlContent = $@"
                <h2>Lasku odottaa katselmoitavana</h2>
                <p>Hei {displayName},</p>
                <p>Lasku <strong>{invoiceTitle}</strong> on siirretty katselmoitavaksi.</p>
                <p>Ole hyvä ja tarkista lasku sekä lisää omat mahdolliset kulusi:</p>
                <p><a href=""{invoiceUrl}"" style=""display: inline-block; padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px;"">Avaa lasku</a></p>
                <p>Tai kopioi linkki selaimeesi:</p>
                <p>{invoiceUrl}</p>
                <br>
                <p>Terveisin,<br>Mökkilan Invoices</p>
            ";

            var plainTextContent = $@"
Lasku odottaa katselmoitavana

Hei {displayName},

Lasku {invoiceTitle} on siirretty katselmoitavaksi.

Ole hyvä ja tarkista lasku sekä lisää omat mahdolliset kulusi:

{invoiceUrl}

Terveisin,
Mökkilan Invoices
            ";

            return await SendEmailAsync(email, displayName, subject, plainTextContent, htmlContent);
        }

        public async Task<bool> SendInvoicePaymentNotificationAsync(string email, string displayName, string invoiceTitle, string invoiceUrl, List<EmailAttachment> attachments)
        {
            var subject = "Lasku siirtynyt maksuun - Mökkilan Invoices";
            var htmlContent = $@"
                <h2>Lasku on siirtynyt maksuun</h2>
                <p>Hei {displayName},</p>
                <p>Lasku <strong>{invoiceTitle}</strong> on hyväksytty ja siirtynyt maksuun.</p>
                <p>Liitteenä löydät:</p>
                <ul>
                    <li>Henkilökohtainen laskusi maksuosuuksineen</li>
                    <li>Kokonaiserittely kaikista kuluista</li>
                </ul>
                <p>Voit myös tarkastella laskua verkossa:</p>
                <p><a href=""{invoiceUrl}"" style=""display: inline-block; padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px;"">Avaa lasku</a></p>
                <p>Tai kopioi linkki selaimeesi:</p>
                <p>{invoiceUrl}</p>
                <br>
                <p>Terveisin,<br>Mökkilan Invoices</p>
            ";

            var plainTextContent = $@"
Lasku on siirtynyt maksuun

Hei {displayName},

Lasku {invoiceTitle} on hyväksytty ja siirtynyt maksuun.

Liitteenä löydät:
- Henkilökohtainen laskusi maksuosuuksineen
- Kokonaiserittely kaikista kuluista

Voit myös tarkastella laskua verkossa:
{invoiceUrl}

Terveisin,
Mökkilan Invoices
            ";

            return await SendEmailWithAttachmentsAsync(email, displayName, subject, plainTextContent, htmlContent, attachments);
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

        private async Task<bool> SendEmailWithAttachmentsAsync(string toEmail, string toName, string subject, string plainTextContent, string htmlContent, List<EmailAttachment> attachments)
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

                // Add attachments if any
                if (attachments != null && attachments.Any())
                {
                    foreach (var attachment in attachments)
                    {
                        bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
                    }
                }

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

                _logger.LogInformation("Email with {AttachmentCount} attachments sent successfully to {Email}", attachments?.Count ?? 0, toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending email with attachments to {Email}", toEmail);
                return false;
            }
        }
    }
}
