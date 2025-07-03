using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public interface IClaimTransformRef<T> where T : ClaimTransform
    {
        List<T> ClaimTransforms { get; set; }
    }
}
