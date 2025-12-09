using FoxIDs.Infrastructure.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Invoice line item used when generating invoices or credit notes.
    /// </summary>
    public class InvoiceLine
    {
        /// <summary>
        /// Description of the line item.
        /// </summary>
        [MaxLength(Constants.Models.Used.InvoiceLineTextLength)]
        public string Text { get; set; }

        /// <summary>
        /// Quantity billed.
        /// </summary>
        [Min(Constants.Models.Used.QuantityMin)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Unit price applied to the quantity.
        /// </summary>
        [Min(Constants.Models.Used.PriceMin)]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Calculated line price.
        /// </summary>
        [Min(Constants.Models.Used.PriceMin)]
        public decimal Price { get; set; }
    }
}
