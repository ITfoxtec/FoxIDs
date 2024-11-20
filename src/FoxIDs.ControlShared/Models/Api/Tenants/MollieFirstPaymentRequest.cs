using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class MollieFirstPaymentRequest
    {
        [MaxLength(Constants.Models.Payment.CardTokenLength)]
        public string CardToken { get; set; }
    }
}
