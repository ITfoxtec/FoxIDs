using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using FoxIDs.Models.Logic;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Logs
{
    public class SendEventEmailLogic : LogicBase
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly SendEmailLogic sendEmailLogic;

        public SendEventEmailLogic(FoxIDsControlSettings settings, TelemetryScopedLogger logger, SendEmailLogic sendEmailLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.sendEmailLogic = sendEmailLogic;
        }

        public async Task SendEventEmailAsync(string eventName, string message) 
        {
            if (!settings.SupportEmail.IsNullOrEmpty())
            {
                try
                {
                    var emailContent = new EmailContent
                    {
                        Subject = $"FoxIDs.Control - {eventName}",
                        Body = message
                    };

                    if (!string.IsNullOrWhiteSpace(settings.Address?.CompanyName))
                    {
                        var aList = new List<string>
                        {
                            settings.Address.CompanyName
                        };
                        if (!settings.Address.AddressLine1.IsNullOrWhiteSpace())
                        {
                            aList.Add(settings.Address.AddressLine1);
                        }
                        if (!settings.Address.AddressLine2.IsNullOrWhiteSpace())
                        {
                            aList.Add(settings.Address.AddressLine2);
                        }
                        if (!settings.Address.PostalCode.IsNullOrWhiteSpace() && !settings.Address.City.IsNullOrWhiteSpace())
                        {
                            aList.Add($"{settings.Address.PostalCode} {settings.Address.City}");
                        }
                        if (!settings.Address.StateRegion.IsNullOrWhiteSpace())
                        {
                            aList.Add(settings.Address.StateRegion);
                        }
                        if (!settings.Address.Country.IsNullOrWhiteSpace())
                        {
                            aList.Add(settings.Address.Country);
                        }

                        emailContent.Address = string.Join(" - ", aList);
                    }

                    await sendEmailLogic.SendEmailAsync(new MailboxAddress(settings.SupportEmail, settings.SupportEmail), emailContent);
                }
                catch (Exception ex)
                {
                    logger.Warning(ex);
                }
            }
        }
    }
}
