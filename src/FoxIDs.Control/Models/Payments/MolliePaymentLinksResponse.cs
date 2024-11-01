using Newtonsoft.Json;

namespace FoxIDs.Models.Payments
{
    public class MolliePaymentLinksResponse
    {
        [JsonProperty(PropertyName = "self")]
        public string Self { get; set; }

        [JsonProperty(PropertyName = "checkout")]
        public string Checkout { get; set; }

        [JsonProperty(PropertyName = "documentation")]
        public string Documentation { get; set; }
    }
}
