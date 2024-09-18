using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class SamlUpPartyProfileViewModel : UpPartyProfileViewModel
    {
        [Display(Name = "Optional Authn context comparison")]
        public SamlAuthnContextComparisonTypesVievModel AuthnContextComparisonViewModel { get; set; }

        [Display(Name = "Optional Authn context class references")]
        public List<string> AuthnContextClassReferences { get; set; } = new List<string>();

        [Display(Name = "Optional Authn request extensions XML")]
        public string AuthnRequestExtensionsXml { get; set; }
    }
}
