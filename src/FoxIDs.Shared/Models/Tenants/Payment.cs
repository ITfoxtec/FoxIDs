using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class Payment
    {
        [JsonProperty(PropertyName = "is_active")]
        public bool IsActive { get; set; }

        [MaxLength(Constants.Models.Payment.CustomerIdLength)]
        [JsonProperty(PropertyName = "customer_id")]
        public string CustomerId { get; set; }

        [MaxLength(Constants.Models.Payment.MandateIdLength)]
        [JsonProperty(PropertyName = "mandate_id")]
        public string MandateId { get; set; }

        [MaxLength(Constants.Models.Payment.CardHolderLength)]
        [JsonProperty(PropertyName = "card_holder")]
        public string CardHolder { get; set; }

        [MaxLength(Constants.Models.Payment.CardNumberInfoLength)]
        [JsonProperty(PropertyName = "card_number_info")]
        public string CardNumberInfo { get; set; }

        [MaxLength(Constants.Models.Payment.CardLabelLength)]
        [JsonProperty(PropertyName = "card_label")]
        public string CardLabel { get; set; }

        [JsonProperty(PropertyName = "card_expiry_month")]
        public int CardExpiryMonth { get; set; }    
        
        [JsonProperty(PropertyName = "card_expiry_year")]
        public int CardExpiryYear { get; set; }
    }
}
