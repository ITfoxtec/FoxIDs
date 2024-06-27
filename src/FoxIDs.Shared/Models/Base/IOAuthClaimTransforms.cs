using System.Collections.Generic;

namespace FoxIDs.Models
{
    public interface IOAuthClaimTransforms
    {
        List<OAuthClaimTransform> ClaimTransforms { get; set; }
    }
}
