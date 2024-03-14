using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IOAuthClaimTransformViewModel
    {
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        List<OAuthClaimTransformViewModel> ClaimTransforms { get; set; }
    }
}
