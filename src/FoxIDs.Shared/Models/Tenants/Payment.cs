using Newtonsoft.Json;

namespace FoxIDs.Models
{
    public class Payment
    {
        [JsonProperty(PropertyName = "is_active")]
        public bool IsActive { get; set; }

        [JsonProperty(PropertyName = "customer_id")]
        public string CustomerId { get; set; }
        
        [JsonProperty(PropertyName = "mandate_id")]
        public string MandateId { get; set; }

        [JsonProperty(PropertyName = "card_holder")]
        public string CardHolder { get; set; }

        [JsonProperty(PropertyName = "card_number")]
        public string CardNumber { get; set; }

        [JsonProperty(PropertyName = "card_label")]
        public string CardLabel { get; set; }

        [JsonProperty(PropertyName = "card_expiry_month")]
        public int CardExpiryMonth { get; set; }    
        
        [JsonProperty(PropertyName = "card_expiry_year")]
        public int CardExpiryYear { get; set; }
    }
}
