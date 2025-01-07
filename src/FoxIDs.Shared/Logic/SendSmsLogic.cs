using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using FoxIDs.Models.Logic;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SendSmsLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly TelemetryScopedLogger logger;

        public SendSmsLogic(Settings settings, TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
        }

        public async Task SendSmsAsync(string phone, SmsContent smsContent)
        {
            throw new NotImplementedException();
        }
    }
}
