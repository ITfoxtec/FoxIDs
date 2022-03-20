namespace FoxIDs.Models
{
    public class TwoFactorSetupInfo
    {
        public string Secret { get; set; }
        public string QrCodeSetupImageUrl { get; set; }
        public string ManualSetupKey { get; set; }
    }
}
