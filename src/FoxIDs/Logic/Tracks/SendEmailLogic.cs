using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SendEmailLogic : LogicBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;

        public SendEmailLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
        }

        public async Task SendEmailAsync(MailAddress toEmail, string subject, string body)
        {
            if (toEmail == null) throw new ArgumentNullException(nameof(toEmail));

            try
            {
                var emailSettings = GetSettings();

                if (!emailSettings.SendgridApiKey.IsNullOrWhiteSpace())
                {
                    logger.ScopeTrace(() => $"Send email with Sendgrid using {(RouteBinding.SendEmail == null ? "default" : "track")} settings .");
                    await SendEmailWithSendgridAsync(emailSettings, toEmail, subject, body);
                }
                else if (!emailSettings.SmtpHost.IsNullOrWhiteSpace())
                {
                    logger.ScopeTrace(() => $"Send email with SMTP using {(RouteBinding.SendEmail == null ? "default" : "track")} settings .");
                    await SendEmailWithSmtpAsync(emailSettings, toEmail, subject, body);
                }
                else
                {
                    //TODO add support for other email providers
                    throw new NotImplementedException("Email provider not supported.");
                }
            }
            catch (EmailConfigurationException cex)
            {
                logger.Warning(cex);
                return;
            }
        }

        private async Task SendEmailWithSendgridAsync(SendEmail emailSettings, MailAddress toEmail, string subject, string body)
        {
            var mail = new SendGridMessage();
            mail.From = new EmailAddress(emailSettings.FromEmail);
            mail.AddTo(toEmail.Address, toEmail.DisplayName);
            mail.Subject = subject;
            mail.AddContent(MediaTypeNames.Text.Html, body);

            var client = new SendGridClient(emailSettings.SendgridApiKey);
            var response = await client.SendEmailAsync(mail);

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                logger.Event($"Email send to '{toEmail.Address}'.");
                logger.ScopeTrace(() => $"Email with subject '{subject}' send to '{toEmail.Address}'.");
            }
            else
            {
                throw new Exception($"Sending email to '{toEmail.Address}' failed with status code '{response.StatusCode}'. Headers '{response.Headers}', body '{await response.Body.ReadAsStringAsync()}'.");
            }
        }

        private async Task SendEmailWithSmtpAsync(SendEmail emailSettings, MailAddress toEmail, string subject, string body)
        {
            try
            {
                var mail = new MailMessage();
                mail.From = new MailAddress(emailSettings.FromEmail);
                mail.To.Add(new MailAddress(toEmail.Address, toEmail.DisplayName, Encoding.UTF8));
                mail.Subject = subject;
                mail.SubjectEncoding = Encoding.UTF8;
                mail.Body = body;
                mail.IsBodyHtml = true;
                mail.BodyEncoding = Encoding.UTF8;

                using var client = new SmtpClient(emailSettings.SmtpHost, emailSettings.SmtpPort);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(emailSettings.SmtpUsername, emailSettings.SmtpPassword);
                await client.SendMailAsync(mail);

                logger.Event($"Email send to '{toEmail.Address}'.");
                logger.ScopeTrace(() => $"Email with subject '{subject}' send to '{toEmail.Address}'.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Sending email to '{toEmail.Address}' failed.", ex);
            }
        }

        private SendEmail GetSettings()
        {
            if (RouteBinding.SendEmail != null)
            {
                return RouteBinding.SendEmail;
            }
            
            if (!string.IsNullOrWhiteSpace(settings.Sendgrid?.FromEmail) && !string.IsNullOrWhiteSpace(settings.Sendgrid?.ApiKey))
            {
                return new SendEmail
                {
                    FromEmail = settings.Sendgrid.FromEmail,
                    SendgridApiKey = settings.Sendgrid.ApiKey
                };
            }

            if (!string.IsNullOrWhiteSpace(settings.Smtp?.FromEmail) && !string.IsNullOrWhiteSpace(settings.Smtp?.Host) && settings.Smtp?.Port > 0 &&
                !string.IsNullOrWhiteSpace(settings.Smtp?.Username) && !string.IsNullOrWhiteSpace(settings.Smtp?.Password))
            {
                return new SendEmail
                {
                    FromEmail = settings.Smtp.FromEmail,
                    SmtpHost = settings.Smtp.Host,
                    SmtpPort = settings.Smtp.Port,
                    SmtpUsername = settings.Smtp.Username,
                    SmtpPassword = settings.Smtp.Password
                };
            }

            throw new EmailConfigurationException("Email settings is not configured.");
        }
    }
}
