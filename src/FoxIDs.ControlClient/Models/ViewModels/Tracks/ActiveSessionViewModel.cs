using FoxIDs.Models.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Client.Models.ViewModels
{
    public class ActiveSessionViewModel : ActiveSession
    {
        [Display(Name = "Authentication method links")]
        public new List<PartyNameSessionLink> UpPartyLinks { get => base.UpPartyLinks; set => base.UpPartyLinks = value; }

        [Display(Name = "Application links")]
        public new List<PartyNameSessionLink> DownPartyLinks { get => base.DownPartyLinks; set => base.DownPartyLinks = value; }

        [Display(Name = "Created at")]
        public string CreatedAtText
        {
            get => DateTimeOffset.FromUnixTimeSeconds(CreateTime).ToUniversalTime().ToLocalTime().ToString();
            set { }
        }

        [Display(Name = "Last updated")]
        public string LastUpdatedText
        {
            get => DateTimeOffset.FromUnixTimeSeconds(LastUpdated).ToUniversalTime().ToLocalTime().ToString();
            set { }
        }

        [Display(Name = "Expire at")]
        public string ExpireAtText
        {
            get => DateTimeOffset.FromUnixTimeSeconds(CreateTime + TimeToLive).ToUniversalTime().ToLocalTime().ToString();
            set { }
        }

        [Display(Name = "Client IP")]
        public string ClientIpDisplay { get => ClientIp; set { } }

        [Display(Name = "User agent")]
        public string UserAgentDisplay { get => UserAgent; set { } }

        [Display(Name = "Authentication methods")]
        public string UpPartyLinksDisplay
        {
            get => UpPartyLinks?.Any() == true ? string.Join(", ", UpPartyLinks.Select(l => $"{l.Name} ({l.Type})")) : string.Empty;
            set { }
        }

        [Display(Name = "Applications")]
        public string DownPartyLinksDisplay
        {
            get => DownPartyLinks?.Any() == true ? string.Join(", ", DownPartyLinks.Select(l => $"{l.Name} ({l.Type})")) : string.Empty;
            set { }
        }
    }
}
