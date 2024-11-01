using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class InvoiceLine
    {
        [MaxLength(Constants.Models.Used.InvoiceLineTextLength)]
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [Min(Constants.Models.Used.QuantityMin)]
        [JsonProperty(PropertyName = "quantity")]
        public double Quantity { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        [JsonProperty(PropertyName = "unit_price")]
        public decimal UnitPrice { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        [JsonProperty(PropertyName = "price")]
        public decimal Price { get; set; }
    }
}
