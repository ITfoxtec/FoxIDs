using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using FoxIDs.Models.Logic;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using MimeKit;
using System;
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
