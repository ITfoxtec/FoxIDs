using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ExternalInvoice
{
    public class InvoiceResponse
    {
        [Required]
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// Base64 encoded PDF invoice.
        /// </summary>
        public string PdfInvoice { get; set; }
    }
}
