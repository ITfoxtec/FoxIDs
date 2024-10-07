using Newtonsoft.Json;
using System;

namespace FoxIDs.Models.Payment
{
    public class MolliePaymentsResponse
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
        public MolliePaymentsDetailsResponse Details { get; set; }

        [JsonProperty(PropertyName = "_links")]
        public MolliePaymentsLinksResponse Links { get; set; }
    }
}
