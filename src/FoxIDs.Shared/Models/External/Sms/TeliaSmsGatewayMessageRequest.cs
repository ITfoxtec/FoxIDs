using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.External.Sms
{
    public class TeliaSmsGatewayMessageRequest
    {
        [JsonProperty("address")]
        public List<string> Address { get; set; }

        [JsonProperty("senderAddress")]
        public string SenderAddress { get; set; }

        [JsonProperty("senderName")]
        public string SenderName { get; set; }

        [JsonProperty("outboundSMSTextMessage")]
        public TeliaSmsGatewayTextMessage OutboundSmsTextMessage { get; set; }
    }
}
