using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class SmsPrice
    {
        [Required]
        [MaxLength(Constants.Models.SmsPrices.CountryNameLength)]
        [JsonProperty(PropertyName = "country_name")]
        public string CountryName { get; set; }

        [Required]
        [MaxLength(Constants.Models.SmsPrices.Iso2Length)]
        [JsonProperty(PropertyName = "iso2")]
        public string Iso2 { get; set; }

        [Required]
        [Min(Constants.Models.SmsPrices.PhoneCodeMin)]
        [JsonProperty(PropertyName = "phone_code")]
        public int PhoneCode { get; set; }

        [Required]
        [Min(Constants.Models.SmsPrices.PriceMin)]
        [JsonProperty(PropertyName = "price")]
        public decimal Price { get; set; }
    }
}
