namespace FoxIDs.Models.Config
{
    public class PaymentSettings
    {
        public bool TestMode { get; set; } = false;
        public bool EnablePayment => !string.IsNullOrWhiteSpace(MollieApiKey) && !string.IsNullOrWhiteSpace(MollieProfileId);

        public string MollieApiKey { get; set; }   
        public string MollieProfileId { get; set; }
        public string MollieApiUrl { get; set; }
    }
}
