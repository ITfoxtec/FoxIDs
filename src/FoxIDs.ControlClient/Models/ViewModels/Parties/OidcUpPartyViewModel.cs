using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OidcUpPartyViewModel : IOAuthClaimTransformViewModel
    {
        public bool IsManual { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Up-party name (client ID)")]
        public string Name { get; set; }

        public bool AutomaticStopped { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.AuthorityLength)]
        [Display(Name = "Authority")]
        public string Authority { get; set; }

        [Display(Name = "Issuer")]
        public string Issuer { get; set; }

        [Display(Name = "Key IDs")]
        public List<string> KeyIds { get; set; } = new List<string>();

        [Range(Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMin, Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMax)]
        [Display(Name = "Automatic update rate")]
        public int OidcDiscoveryUpdateRate { get; set; } = 2592000; // 30 days

        /// <summary>
        /// OIDC up client.
        /// </summary>
        [Required]
        [ValidateComplexType]
        public OidcUpClient Client { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransformViewModel> ClaimTransforms { get; set; } = new List<OAuthClaimTransformViewModel>();
    }
}
