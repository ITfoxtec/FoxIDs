using Newtonsoft.Json;

namespace FoxIDs.Models.Payment
{
    public class MolliePaymentsDetailsResponse
    {
        [JsonProperty(PropertyName = "failureReason")]
        public string FailureReason { get; set; }

        [JsonProperty(PropertyName = "failureMessage")]
        public string FailureMessage { get; set; }
    }
}
