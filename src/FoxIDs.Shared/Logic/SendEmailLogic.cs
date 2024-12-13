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
    public class SendEmailLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly TelemetryScopedLogger logger;

        public SendEmailLogic(Settings settings, TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
        }

        public async Task SendEmailAsync(MailboxAddress toEmail, EmailContent emailContent)
        {
            if (toEmail == null) throw new ArgumentNullException(nameof(toEmail));

            try
            {
                var emailSettings = GetSettings();

                if (!emailSettings.SendgridApiKey.IsNullOrWhiteSpace())
                {
                    logger.ScopeTrace(() => $"Send email with Sendgrid using {(RouteBinding.SendEmail == null ? "default" : "environment")} settings.");
                    await SendEmailWithSendgridAsync(emailSettings, toEmail, emailContent.Subject, GetBodyHtml(emailContent));
                }
                else if (!emailSettings.SmtpHost.IsNullOrWhiteSpace())
                {
                    logger.ScopeTrace(() => $"Send email with SMTP using {(RouteBinding.SendEmail == null ? "default" : "environment")} settings.");
                    await SendEmailWithSmtpAsync(emailSettings, toEmail, emailContent.Subject, GetBodyHtml(emailContent));
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

        private string GetBodyHtml(EmailContent emailContent)
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
  <body>
    <table border=""0"" cellpadding=""5"" cellspacing=""0"" width=""100%"">
      <tbody>
        <tr>
            <td>
{1}
              <table border=""0"" cellpadding=""5"" cellspacing=""0"" width=""100%"">
                <tbody>
                  <tr><td style=""height: 40px;"">&nbsp;</td></tr>
                  {2}
                  <tr>
                    <td>
                      <div align=""center"" style=""color:#8f8f8f"">
                        {3}
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </td>
        </tr>
      </tbody>
    </table>
  </body>
</html>", emailContent.ParentCulture, BodyHtmlEncode(emailContent.Body), BodyHtmlEncode(GetInfoHtml(emailContent.Info)), emailContent.Address); 
            return bodyHtml;
        }

        private string GetInfoHtml(string info)
        {
            if(info.IsNullOrEmpty())
            {
                return string.Empty;
            }

            var infoWithLinks = string.Format(info, "https://www.foxids.com/", "https://www.itfoxtec.com/");

            var infoHtml = string.Format(
@"                  <tr>
                    <td>
                      <div align=""center"" style=""color:#8f8f8f"">
                        {0}
                      </div>
                    </td>
                  </tr>", info);
            return infoHtml;
        }

        private string BodyHtmlEncode(string body)
        {
            //body = HttpUtility.HtmlEncode(body);
            //body = body.Replace("&lt;", "<");
            //body = body.Replace("&gt;", ">");
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
