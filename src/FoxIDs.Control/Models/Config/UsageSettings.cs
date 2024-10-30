using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class UsageSettings
    {
        /// <summary>
        /// The usage calculator background service wait period in seconds.
        /// </summary>
        [Required]
        public int BackgroundServiceWaitPeriod { get; set; } = 7200; // 2 hours

        public bool EnableInvoice => !string.IsNullOrWhiteSpace(ExternalInvoiceApiId) && !string.IsNullOrWhiteSpace(ExternalInvoiceApiSecret) && !string.IsNullOrWhiteSpace(ExternalInvoiceApiUrl);

        public string ExternalInvoiceApiId { get; set; } = "external_invoice";

        public string ExternalInvoiceApiSecret { get; set; }

        public string ExternalInvoiceApiUrl { get; set; }
    }
}
