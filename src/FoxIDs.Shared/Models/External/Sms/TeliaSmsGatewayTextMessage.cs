using Newtonsoft.Json;

namespace FoxIDs.Models.External.Sms
{
    public class TeliaSmsGatewayTextMessage
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
