using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IClaimTransformViewModel
    { 
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        List<ClaimTransformViewModel> ClaimTransforms { get; set; }
    }
}
