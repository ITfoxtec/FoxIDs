using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class Customer
    {
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        public string InvoiceEmail { get; set; }

        [MaxLength(Constants.Models.Customer.ReferenceLength)]
        [JsonProperty(PropertyName = "reference")]
        public string Reference { get; set; }

        /// <summary>
        /// Company name or name.
        /// </summary>
        [MaxLength(Constants.Models.Customer.NameLength)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Customer.VatNumberLength)]
        [JsonProperty(PropertyName = "vat_number")]
        public string VatNumber { get; set; }

        [MaxLength(Constants.Models.Customer.AddressLine1Length)]
        [JsonProperty(PropertyName = "address_line_1")]
        public string AddressLine1 { get; set; }

        [MaxLength(Constants.Models.Customer.AddressLine2Length)]
        [JsonProperty(PropertyName = "address_line_2")]
        public string AddressLine2 { get; set; }

        [MaxLength(Constants.Models.Customer.PostalCodeLength)]
        [JsonProperty(PropertyName = "postal_code")]
        public string PostalCode { get; set; }

        [MaxLength(Constants.Models.Customer.CityLength)]
        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        [MaxLength(Constants.Models.Customer.StateRegionLength)]
        [JsonProperty(PropertyName = "state_region")]
        public string StateRegion { get; set; }

        [MaxLength(Constants.Models.Customer.CountryLength)]
        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }
    }
}
