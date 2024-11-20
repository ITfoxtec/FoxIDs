using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ExternalInvoices
{
    public class Customer : Address
    {
        [ListLength(Constants.Models.Customer.InvoiceEmailsMin, Constants.Models.Customer.InvoiceEmailsMax, Constants.Models.User.EmailLength, Constants.Models.User.EmailRegExPattern)]
        public List<string> InvoiceEmails { get; set; }

        [MaxLength(Constants.Models.Customer.ReferenceLength)]
        public string Reference { get; set; }       
    }
}
