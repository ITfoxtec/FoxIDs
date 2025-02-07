using Newtonsoft.Json;

namespace FoxIDs.Models
{
    public class SmsPrice
    {
        [JsonProperty(PropertyName = "country_name")]
        public string CountryName { get; set; }

        [JsonProperty(PropertyName = "iso2")]
        public string Iso2 { get; set; }

        [JsonProperty(PropertyName = "phone_code")]
        public int PhoneCode { get; set; }

        [JsonProperty(PropertyName = "price")]
        public decimal Price { get; set; }
    }
}
