using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class Payment
    {
        public bool IsActive { get; set; }

        [JsonProperty(PropertyName = "card_holder")]
        public string CardHolder { get; set; }

        [MaxLength(Constants.Models.Payment.CardNumberLength)]
        public string CardNumber { get; set; }

        [MaxLength(Constants.Models.Payment.CardLabelLength)]
        public string CardLabel { get; set; }

        public int CardExpiryMonth { get; set; }

        public int CardExpiryYear { get; set; }
    }
}
