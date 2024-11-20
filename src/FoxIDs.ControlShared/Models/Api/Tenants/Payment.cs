using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class Payment
    {
        public bool IsActive { get; set; }

        public string CardHolder { get; set; }

        [MaxLength(Constants.Models.Payment.CardNumberInfoLength)]
        public string CardNumberInfo { get; set; }

        [MaxLength(Constants.Models.Payment.CardLabelLength)]
        public string CardLabel { get; set; }

        public int CardExpiryMonth { get; set; }

        public int CardExpiryYear { get; set; }
    }
}
