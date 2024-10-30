using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class Customer
    {
        [ListLength(Constants.Models.Customer.InvoiceEmailsMin, Constants.Models.Customer.InvoiceEmailsMax, Constants.Models.User.EmailLength, Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Invoice emails")]
        public List<string> InvoiceEmails { get; set; }

        [MaxLength(Constants.Models.Customer.ReferenceLength)]
        [Display(Name = "Reference")]
        public string Reference { get; set; }

        /// <summary>
        /// Company name or name.
        /// </summary>
        [MaxLength(Constants.Models.Customer.NameLength)]
        [Display(Name = "Company name / Name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Customer.VatNumberLength)]
        [Display(Name = "VAT number")]
        public string VatNumber { get; set; }

        [MaxLength(Constants.Models.Customer.AddressLine1Length)]
        [Display(Name = "Address line 1")]
        public string AddressLine1 { get; set; }

        [MaxLength(Constants.Models.Customer.AddressLine2Length)]
        [Display(Name = "Address line 2")]
        public string AddressLine2 { get; set; }

        [MaxLength(Constants.Models.Customer.PostalCodeLength)]
        [Display(Name = "Postal code")]
        public string PostalCode { get; set; }

        [MaxLength(Constants.Models.Customer.CityLength)]
        [Display(Name = "City")]
        public string City { get; set; }

        [MaxLength(Constants.Models.Customer.StateRegionLength)]
        [Display(Name = "State / Region")]
        public string StateRegion { get; set; }

        [MaxLength(Constants.Models.Customer.CountryLength)]
        [Display(Name = "Country")]
        public string Country { get; set; }           
    }
}
