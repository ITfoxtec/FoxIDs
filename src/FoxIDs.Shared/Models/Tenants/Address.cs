using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public abstract class Address
    {
        /// <summary>
        /// Company name or name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Address.NameLength)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Address.VatNumberLength)]
        [JsonProperty(PropertyName = "vat_number")]
        public string VatNumber { get; set; }

        [MaxLength(Constants.Models.Address.AddressLine1Length)]
        [JsonProperty(PropertyName = "address_line_1")]
        public string AddressLine1 { get; set; }

        [MaxLength(Constants.Models.Address.AddressLine2Length)]
        [JsonProperty(PropertyName = "address_line_2")]
        public string AddressLine2 { get; set; }

        [MaxLength(Constants.Models.Address.PostalCodeLength)]
        [JsonProperty(PropertyName = "postal_code")]
        public string PostalCode { get; set; }

        [MaxLength(Constants.Models.Address.CityLength)]
        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        [MaxLength(Constants.Models.Address.StateRegionLength)]
        [JsonProperty(PropertyName = "state_region")]
        public string StateRegion { get; set; }

        [MaxLength(Constants.Models.Address.CountryLength)]
        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }
    }
}
