using Newtonsoft.Json;

namespace FoxIDs.Models.External.Sms
{
    public class TeliaSmsGatewayRequest
    {
        [JsonProperty("outboundMessageRequest")]
        public TeliaSmsGatewayMessageRequest OutboundMessageRequest { get; set; }
    }
}
