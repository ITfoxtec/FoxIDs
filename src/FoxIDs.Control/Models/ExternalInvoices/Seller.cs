using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ExternalInvoices
{
    public class Seller : Address
    {
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        public string FromEmail { get; set; }

        [ListLength(Constants.Models.Seller.BccEmailsMin, Constants.Models.Seller.BccEmailsMax, Constants.Models.User.EmailLength, Constants.Models.User.EmailRegExPattern)]
        public List<string> BccEmails { get; set; }
    }
}
