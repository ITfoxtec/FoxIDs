using FoxIDs.Infrastructure.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class InvoiceLine
    {
        [MaxLength(Constants.Models.Used.InvoiceLineTextLength)]
        public string Text { get; set; }

        [Min(Constants.Models.Used.QuantityMin)]
        public double Quantity { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        public decimal UnitPrice { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        public decimal Price { get; set; }
    }
}
