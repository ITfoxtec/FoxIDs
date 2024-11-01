using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public abstract class Address
    {
        /// <summary>
        /// Company name or name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Address.NameLength)]
        [Display(Name = "Company name / Name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Address.VatNumberLength)]
        [Display(Name = "VAT number")]
        public string VatNumber { get; set; }

        [MaxLength(Constants.Models.Address.AddressLine1Length)]
        [Display(Name = "Address line 1")]
        public string AddressLine1 { get; set; }

        [MaxLength(Constants.Models.Address.AddressLine2Length)]
        [Display(Name = "Address line 2")]
        public string AddressLine2 { get; set; }

        [MaxLength(Constants.Models.Address.PostalCodeLength)]
        [Display(Name = "Postal code")]
        public string PostalCode { get; set; }

        [MaxLength(Constants.Models.Address.CityLength)]
        [Display(Name = "City")]
        public string City { get; set; }

        [MaxLength(Constants.Models.Address.StateRegionLength)]
        [Display(Name = "State / Region")]
        public string StateRegion { get; set; }

        [MaxLength(Constants.Models.Address.CountryLength)]
        [Display(Name = "Country")]
        public string Country { get; set; }

    }
}
