using Newtonsoft.Json;

namespace FoxIDs.Models.Payment
{
    public class MolliePaymentsLinkResponse
    {
        [JsonProperty(PropertyName = "ref")]
        public string Href { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }
}
