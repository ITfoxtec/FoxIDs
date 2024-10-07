using Newtonsoft.Json;

namespace FoxIDs.Models.Payment
{
    public class MolliePaymentsLinksResponse
    {
        [JsonProperty(PropertyName = "self")]
        public string Self { get; set; }

        [JsonProperty(PropertyName = "checkout")]
        public string Checkout { get; set; }

        [JsonProperty(PropertyName = "documentation")]
        public string Documentation { get; set; }
    }
}
