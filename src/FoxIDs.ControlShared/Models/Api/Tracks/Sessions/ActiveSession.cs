using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ActiveSession
    {
        [Required]
        [MaxLength(IdentityConstants.MessageLength.SessionIdMax)]
        [Display(Name = "Session ID")]
        public string SessionId { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "SUB claim")]
        public string Sub { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "SUB claim format")]
        public string SubFormat { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Display(Name = "Authentication methods")]
        public List<PartyNameSessionLink> UpPartyLinks { get; set; }

        [Display(Name = "Authentication session method")]
        public PartyNameSessionLink SessionUpParty { get; set; }

        [Display(Name = "Applications")]
        public List<PartyNameSessionLink> DownPartyLinks { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Client IP")]
        public string ClientIp { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "User agent")]
        public string UserAgent { get; set; }

        [Required]
        [Display(Name = "Create time")]
        public long CreateTime { get; set; }

        [Display(Name = "Last updated")]
        public long LastUpdated { get; set; }

        [Display(Name = "Time to live")]
        public int TimeToLive { get; set; }
    }
}
