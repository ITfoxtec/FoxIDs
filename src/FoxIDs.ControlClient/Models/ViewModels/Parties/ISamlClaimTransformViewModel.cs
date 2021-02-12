using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface ISamlClaimTransformViewModel
    {
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [Display(Name = "Claim transforms executed in order")]
        List<SamlClaimTransformViewModel> ClaimTransforms { get; set; }
    }
}
