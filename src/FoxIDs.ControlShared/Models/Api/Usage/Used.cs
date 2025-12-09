using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Aggregated usage for a tenant within a billing period.
    /// </summary>
    public class Used : UsedBase, INameValue
    {
        /// <summary>
        /// Number of tracks in use.
        /// </summary>
        [Display(Name = "Tracks")]
        public decimal Tracks { get; set; }

        /// <summary>
        /// Number of active users.
        /// </summary>
        [Display(Name = "Users")]
        public decimal Users { get; set; }

        /// <summary>
        /// Count of sign-ins performed.
        /// </summary>
        [Display(Name = "Logins")]
        public decimal Logins { get; set; }

        /// <summary>
        /// Issued token request count.
        /// </summary>
        [Display(Name = "Token requests")]
        public decimal TokenRequests { get; set; }

        /// <summary>
        /// SMS messages sent.
        /// </summary>
        [Display(Name = "SMS")]
        public decimal Sms { get; set; }

        /// <summary>
        /// Price applied per SMS.
        /// </summary>
        [Display(Name = "SMS country EUR price")]
        public decimal SmsPrice { get; set; }

        /// <summary>
        /// Emails sent.
        /// </summary>
        [Display(Name = "Emails")]
        public decimal Emails { get; set; }

        /// <summary>
        /// GET calls to the Control API.
        /// </summary>
        [Display(Name = "Control API gets")]
        public decimal ControlApiGets { get; set; }

        /// <summary>
        /// POST/PUT/DELETE calls to the Control API.
        /// </summary>
        [Display(Name = "Control API updates")]
        public decimal ControlApiUpdates { get; set; }

        /// <summary>
        /// Detailed usage lines.
        /// </summary>
        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMax)]
        public List<UsedItem> Items { get; set; }

        /// <summary>
        /// Identifier composed of tenant name and period.
        /// </summary>
        public string Name { get => $"{TenantName}/{PeriodBeginDate.Year}/{PeriodBeginDate.Month}"; set => _ = string.Empty; }
    }
}
