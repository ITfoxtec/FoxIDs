using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public interface IExitClaimTransformsRef<T> where T : ClaimTransform
    {
        List<T> ExitClaimTransforms { get; set; }
    }
}
