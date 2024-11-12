using Newtonsoft.Json;

namespace FoxIDs.Models.Payments
{
    public class MolliePaymentDetailsResponse
    {
        [JsonProperty(PropertyName = "failureReason")]
        public string FailureReason { get; set; }

        [JsonProperty(PropertyName = "failureMessage")]
        public string FailureMessage { get; set; }
    }
}
