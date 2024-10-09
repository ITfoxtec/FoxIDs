using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class Customer
    {
        /// <summary>
        /// Company name or name.
        /// </summary>
        [MaxLength(Constants.Models.Customer.NameLength)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Customer.VatNumberLength)]
        public string VatNumber { get; set; }

        [MaxLength(Constants.Models.Customer.AddressLine1Length)]
        public string AddressLine1 { get; set; }

        [MaxLength(Constants.Models.Customer.AddressLine2Length)]
        public string AddressLine2 { get; set; }

        [MaxLength(Constants.Models.Customer.PostalCodeLength)]
        public string PostalCode { get; set; }

        [MaxLength(Constants.Models.Customer.CityLength)]
        public string City { get; set; }

        [MaxLength(Constants.Models.Customer.StateRegionLength)]
        public string StateRegion { get; set; }

        [MaxLength(Constants.Models.Customer.CountryLength)]
        public string Country { get; set; }
    }
}
