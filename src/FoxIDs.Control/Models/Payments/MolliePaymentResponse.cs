using Newtonsoft.Json;
using System;

namespace FoxIDs.Models.Payments
{
    public class MolliePaymentResponse
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "mode")]
        public string Mode { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty(PropertyName = "expiresAt")]
        public DateTimeOffset ExpiresAt { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "details")]
        public MolliePaymentDetailsResponse Details { get; set; }

        [JsonProperty(PropertyName = "_links")]
        public MolliePaymentLinksResponse Links { get; set; }
    }
}
