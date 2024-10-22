using FoxIDs.Infrastructure.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class InvoiceLine
    {
        [MaxLength(Constants.Models.Used.InvoiceLineTextLength)]
        public string Text { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        public double Price { get; set; }
    }
}
