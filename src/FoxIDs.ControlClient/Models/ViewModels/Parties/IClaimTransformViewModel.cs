using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IClaimTransformViewModel
    { 
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        List<ClaimTransformViewModel> ClaimTransforms { get; set; }
    }
}
