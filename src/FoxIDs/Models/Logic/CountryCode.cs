using Newtonsoft.Json;

namespace FoxIDs.Models.Logic
{
    public class CountryCode
    {
        [JsonProperty(PropertyName = "country_name")]
        public string CountryName { get; set; }

        [JsonProperty(PropertyName = "iso2")]
        public string Iso2 { get; set; }

        [JsonProperty(PropertyName = "iso3")]
        public string Iso3 { get; set; }

        [JsonProperty(PropertyName = "fips")]
        public string Fips { get; set; }

        [JsonProperty(PropertyName = "phone_code")]
        public string PhoneCode { get; set; }

        [JsonProperty(PropertyName = "language_codes")]
        public string LanguageCodes { get; set; }
    }
}
