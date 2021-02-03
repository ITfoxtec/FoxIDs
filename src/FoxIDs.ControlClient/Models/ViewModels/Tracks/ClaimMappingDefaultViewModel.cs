using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Client.Models.ViewModels
{
    public class ClaimMappingDefaultViewModel
    {
        [Display(Name = "Default mappings between JWT and SAML claim types")]
        public IEnumerable<ClaimMap> DefaultClaimMappings { get; set; } = Constants.DefaultClaimMappings.LockedMappings.Select(cm => new ClaimMap { JwtClaim = cm.JwtClaim, SamlClaim = cm.SamlClaim });
    }
}
