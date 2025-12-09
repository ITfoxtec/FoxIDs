using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Details about a user's active session across parties.
    /// </summary>
    public class ActiveSession
    {
        /// <summary>
        /// Session identifier.
        /// </summary>
        [Required]
        [MaxLength(IdentityConstants.MessageLength.SessionIdMax)]
        [Display(Name = "Session ID")]
        public string SessionId { get; set; }

        /// <summary>
        /// Subject claim value.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "SUB claim")]
        public string Sub { get; set; }

        /// <summary>
        /// Subject format.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "SUB claim format")]
        public string SubFormat { get; set; }

        /// <summary>
        /// Email associated with the session.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        /// <summary>
        /// Phone number associated with the session.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        /// <summary>
        /// Username associated with the session.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Username")]
        public string Username { get; set; }

        /// <summary>
        /// Upstream party links used during authentication.
        /// </summary>
        [Display(Name = "Authentication methods")]
        public List<PartyNameSessionLink> UpPartyLinks { get; set; }

        /// <summary>
        /// Active authentication session link.
        /// </summary>
        [Display(Name = "Authentication session method")]
        public PartyNameSessionLink SessionUpParty { get; set; }

        /// <summary>
        /// Downstream applications connected to the session.
        /// </summary>
        [Display(Name = "Applications")]
        public List<PartyNameSessionLink> DownPartyLinks { get; set; }

        /// <summary>
        /// Client IP address.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Client IP")]
        public string ClientIp { get; set; }

        /// <summary>
        /// User agent string.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "User agent")]
        public string UserAgent { get; set; }

        /// <summary>
        /// Session creation time (Unix seconds).
        /// </summary>
        [Required]
        [Display(Name = "Create time")]
        public long CreateTime { get; set; }

        /// <summary>
        /// Last updated time (Unix seconds).
        /// </summary>
        [Display(Name = "Last updated")]
        public long LastUpdated { get; set; }

        /// <summary>
        /// Time to live in seconds.
        /// </summary>
        [Display(Name = "Time to live")]
        public int TimeToLive { get; set; }
    }
}
