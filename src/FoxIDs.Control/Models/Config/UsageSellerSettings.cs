using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class UsageSellerSettings
    {
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        public string FromEmail { get; set; }

        [ListLength(Constants.Models.Seller.BccEmailsMin, Constants.Models.Seller.BccEmailsMax, Constants.Models.User.EmailLength, Constants.Models.User.EmailRegExPattern)]
        public List<string> BccEmails { get; set; }

        /// <summary>
        /// Company name or name.
        /// </summary>
        [MaxLength(Constants.Models.Address.NameLength)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Address.VatNumberLength)]
        public string VatNumber { get; set; }

        [MaxLength(Constants.Models.Address.AddressLine1Length)]
        public string AddressLine1 { get; set; }

        [MaxLength(Constants.Models.Address.AddressLine2Length)]
        public string AddressLine2 { get; set; }

        [MaxLength(Constants.Models.Address.PostalCodeLength)]
        public string PostalCode { get; set; }

        [MaxLength(Constants.Models.Address.CityLength)]
        public string City { get; set; }

        [MaxLength(Constants.Models.Address.StateRegionLength)]
        public string StateRegion { get; set; }

        [MaxLength(Constants.Models.Address.CountryLength)]
        public string Country { get; set; }

        public int PaymentDueDays { get; set; } = 10;

        public List<string> BankDetails { get; set; }
    }
}
