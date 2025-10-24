using System.Collections.Generic;

namespace FoxIDs.Models.External.Sms
{
    public class GatewayApiRequest
    {
        public string Message { get; set; }
        public List<GatewayApiRecipient> Recipients { get; set; }
        public string Sender { get; set; }
        public string Label { get; set; }
    }
}
