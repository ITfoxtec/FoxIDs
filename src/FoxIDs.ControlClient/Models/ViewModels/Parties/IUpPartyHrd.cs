using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IUpPartyHrd
    {
        [Display(Name = "Home Realm Discovery (HRD) Domains")]
        public List<string> HrdDomains { get; set; }

        [Display(Name = "Home Realm Discovery (HRD) Display name")]
        public string HrdDisplayName { get; set; }

        [Display(Name = "Home Realm Discovery (HRD) Logo URL")]
        public string HrdLogoUrl { get; set; }
    }
}
