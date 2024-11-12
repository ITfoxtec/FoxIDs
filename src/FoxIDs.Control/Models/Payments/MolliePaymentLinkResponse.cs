using Newtonsoft.Json;

namespace FoxIDs.Models.Payments
{
    public class MolliePaymentLinkResponse
    {
        [JsonProperty(PropertyName = "ref")]
        public string Href { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }
}
