using System.Collections.Generic;

namespace FoxIDs.Models
{
    public interface IOAuthClaimTransformsRef
    {
        List<OAuthClaimTransform> ClaimTransforms { get; set; }
    }
}
