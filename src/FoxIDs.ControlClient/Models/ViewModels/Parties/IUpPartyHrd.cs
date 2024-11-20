using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IUpPartyHrd
    {
        [Display(Name = "HRD Domains (use * to accept all domains not configured on another authentication method)")]
        public List<string> HrdDomains { get; set; }

        [Display(Name = "Show HRD button together with HRD domain")]
        public bool HrdShowButtonWithDomain { get; set; }

        [Display(Name = "Home Realm Discovery (HRD) Display name")]
        public string HrdDisplayName { get; set; }

        [Display(Name = "Home Realm Discovery (HRD) Logo URL")]
        public string HrdLogoUrl { get; set; }
    }
}
