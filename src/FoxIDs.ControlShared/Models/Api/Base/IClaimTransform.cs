using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public interface IClaimTransform<T> where T : ClaimTransform
    {
        List<T> ClaimTransforms { get; set; }
    }
}
