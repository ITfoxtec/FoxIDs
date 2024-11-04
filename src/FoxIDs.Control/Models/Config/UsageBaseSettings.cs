using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class UsageBaseSettings
    {
        /// <summary>
        /// The usage calculator background service wait period in seconds.
        /// </summary>
        public int BackgroundServiceWaitPeriod { get; set; } = 7200; // 2 hours

        [Required]
        public UsageSellerSettings Seller { get; set; }

        /// <summary>
        /// Default VAR percent.
        /// </summary>
        public int VatPercent { get; set; } = 25;

        public bool EnableInvoice => !string.IsNullOrWhiteSpace(ExternalInvoiceApiId) && !string.IsNullOrWhiteSpace(ExternalInvoiceApiSecret) && !string.IsNullOrWhiteSpace(ExternalInvoiceApiUrl);

        public string ExternalInvoiceApiId { get; set; } = "external_invoice";

        public string ExternalInvoiceApiSecret { get; set; }

        public string ExternalInvoiceApiUrl { get; set; }
    }
}
