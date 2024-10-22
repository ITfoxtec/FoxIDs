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

        [JsonProperty(PropertyName = "price")]
        [Min(Constants.Models.Used.PriceMin)]
        public double Price { get; set; }
    }
}
