using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class PlanPaymentSettings
    {
        [MaxLength(Constants.Models.Plan.CurrencyLength)]
        [RegularExpression(Constants.Models.Plan.CurrencyRegExPattern)]
        public string Currency { get; set; } = "EUR";

        public bool TestMode { get; set; } = false;

        public string MollieApiKey { get; set; }   
        public string MollieProfileId { get; set; }
    }
}
