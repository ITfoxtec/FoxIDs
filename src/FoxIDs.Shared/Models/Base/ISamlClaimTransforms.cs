using System.Collections.Generic;

namespace FoxIDs.Models
{
    public interface ISamlClaimTransforms
    {
        List<SamlClaimTransform> ClaimTransforms { get; set; }
    }
}
