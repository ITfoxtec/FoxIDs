using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Customer contact and invoicing preferences.
    /// </summary>
    public class Customer : Address
    {
        /// <summary>
        /// Email addresses that should receive invoices.
        /// </summary>
        [ListLength(Constants.Models.Customer.InvoiceEmailsMin, Constants.Models.Customer.InvoiceEmailsMax, Constants.Models.User.EmailLength, Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Invoice emails")]
        public List<string> InvoiceEmails { get; set; }

        /// <summary>
        /// Optional customer reference to include on invoices.
        /// </summary>
        [MaxLength(Constants.Models.Customer.ReferenceLength)]
        [Display(Name = "Your reference")]
        public string Reference { get; set; }
    }
}
