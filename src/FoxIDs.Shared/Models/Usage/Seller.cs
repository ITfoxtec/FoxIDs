using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class Seller : Address
    {
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [JsonProperty(PropertyName = "from_email")]
        public string FromEmail { get; set; }

        [ListLength(Constants.Models.Seller.BccEmailsMin, Constants.Models.Seller.BccEmailsMax, Constants.Models.User.EmailLength, Constants.Models.User.EmailRegExPattern)]
        [JsonProperty(PropertyName = "bcc_emails")]
        public List<string> BccEmails { get; set; }
    }
}
