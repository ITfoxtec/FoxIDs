using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IOAuthClaimTransformViewModel
    {
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [Display(Name = "Claim transforms executed in order (use the move up and down arrows to change the order)")]
        List<OAuthClaimTransformViewModel> ClaimTransforms { get; set; }
    }
}
