﻿using FoxIDs.Infrastructure;
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

            var emailSettings = GetSettings();
            if(emailSettings.FromEmail.IsNullOrWhiteSpace() || emailSettings.SendgridApiKey.IsNullOrWhiteSpace())
            {
                try
                {
                    throw new Exception("Email settings is not configured.");
                }
                catch (Exception ex)
                {
                    logger.Warning(ex);
                }
                return;
            }

            if (!emailSettings.SendgridApiKey.IsNullOrWhiteSpace())
            {
                logger.ScopeTrace(() => $"Send email with Sendgrid using {(RouteBinding.SendEmail == null ? "default" : "track")} settings .");
                await SendEmailWithSendgridAsync(emailSettings, toEmail, subject, body);
            }
            else
            {
                //TODO add support for other email providers
                throw new NotImplementedException("Email provider not supported.");
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

        private SendEmail GetSettings()
        {
            return RouteBinding.SendEmail ?? new SendEmail
            {
                FromEmail = settings.Sendgrid?.FromEmail,
                SendgridApiKey = settings.Sendgrid?.ApiKey
            };
        }
    }
}
