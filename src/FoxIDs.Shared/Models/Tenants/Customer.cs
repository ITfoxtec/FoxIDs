using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class Customer : Address
    {
        [ListLength(Constants.Models.Customer.InvoiceEmailsMin, Constants.Models.Customer.InvoiceEmailsMax, Constants.Models.User.EmailLength, Constants.Models.User.EmailRegExPattern)]
        [JsonProperty(PropertyName = "invoice_emails")]
        public List<string> InvoiceEmails { get; set; }

        [MaxLength(Constants.Models.Customer.ReferenceLength)]
        [JsonProperty(PropertyName = "reference")]
        public string Reference { get; set; }
    }
}
