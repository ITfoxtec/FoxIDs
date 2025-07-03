using System.Collections.Generic;

namespace FoxIDs.Models
{
    public interface ISamlClaimTransformsRef
    {
        List<SamlClaimTransform> ClaimTransforms { get; set; }
    }
}
