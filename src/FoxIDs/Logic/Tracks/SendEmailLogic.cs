using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Logic;
using ITfoxtec.Identity;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web;

namespace FoxIDs.Logic
{
    public class SendEmailLogic : LogicSequenceBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IHttpContextAccessor httpContextAccessor;

        public SendEmailLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task SendEmailAsync(MailboxAddress toEmail, EmailContent emailContent, string fromName = null)
        {
            if (toEmail == null) throw new ArgumentNullException(nameof(toEmail));

            try
            {
                var emailSettings = GetSettings();
                if (emailSettings.FromName.IsNullOrEmpty() && !fromName.IsNullOrWhiteSpace())
                {
                    emailSettings.FromName = fromName;
                }

                if (!emailSettings.SendgridApiKey.IsNullOrWhiteSpace())
                {
                    logger.ScopeTrace(() => $"Send email with Sendgrid using {(RouteBinding.SendEmail == null ? "default" : "environment")} settings.");
                    await SendEmailWithSendgridAsync(emailSettings, toEmail, emailContent.Subject, GetBodyHtml(emailContent.Body));
                }
                else if (!emailSettings.SmtpHost.IsNullOrWhiteSpace())
                {
                    logger.ScopeTrace(() => $"Send email with SMTP using {(RouteBinding.SendEmail == null ? "default" : "environment")} settings.");
                    await SendEmailWithSmtpAsync(emailSettings, toEmail, emailContent.Subject, GetBodyHtml(emailContent.Body));
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

        private string GetBodyHtml(string body)
        {
            var bodyHtml = string.Format(
@"<!DOCTYPE html>
<html>
  <head lang=""{0}"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""/>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"" />
    <meta name=""x-apple-disable-message-reformatting"">
    <title></title>
    <style type=""text/css"">
      body {{
        margin: 0;
        padding: 0;
        font-family: Arial, sans-serif;
      }}

      table {{
        border-collapse: separate;
      }}
        table td {{
          vertical-align: top; 
        }}

      p {{
        margin: 0;
      }}
    </style>
  </head>
  <body>{1}</body>
</html>", httpContextAccessor.HttpContext.GetCultureParentName(), BodyHtmlEncode(body)); 
            return bodyHtml;
        }

        private string BodyHtmlEncode(string body)
        {
            body = HttpUtility.HtmlEncode(body);
            body = body.Replace("&lt;", "<");
            body = body.Replace("&gt;", ">");
            return body;
        }

        private async Task SendEmailWithSendgridAsync(SendEmail emailSettings, MailboxAddress toEmail, string subject, string body)
        {
            Debug.WriteLine($"HTML: '{body}'");

            var mail = new SendGridMessage();
            mail.From = emailSettings.FromName.IsNullOrWhiteSpace() ? new EmailAddress(emailSettings.FromEmail) : new EmailAddress(emailSettings.FromEmail, emailSettings.FromName);
            mail.AddTo(toEmail.Address, toEmail.Name);
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

        private async Task SendEmailWithSmtpAsync(SendEmail emailSettings, MailboxAddress toEmail, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(emailSettings.FromName.IsNullOrWhiteSpace() ? new MailboxAddress(emailSettings.FromEmail, emailSettings.FromEmail) : new MailboxAddress(emailSettings.FromName, emailSettings.FromEmail));
                message.To.Add(toEmail);
                message.Subject = subject;
                message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

                using var client = new SmtpClient();
                client.Connect(emailSettings.SmtpHost, emailSettings.SmtpPort, SecureSocketOptions.Auto);
                client.Authenticate(emailSettings.SmtpUsername, emailSettings.SmtpPassword);
                await client.SendAsync(message);
                client.Disconnect(true);

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
                    FromName = settings.Sendgrid.FromName,
                    FromEmail = settings.Sendgrid.FromEmail,
                    SendgridApiKey = settings.Sendgrid.ApiKey
                };
            }

            if (!string.IsNullOrWhiteSpace(settings.Smtp?.FromEmail) && !string.IsNullOrWhiteSpace(settings.Smtp?.Host) && settings.Smtp?.Port > 0 &&
                !string.IsNullOrWhiteSpace(settings.Smtp?.Username) && !string.IsNullOrWhiteSpace(settings.Smtp?.Password))
            {
                return new SendEmail
                {
                    FromName = settings.Smtp.FromName,
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
