using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Indicates that a message contains claim transformation rules.
    /// </summary>
    public interface IClaimTransformRef<T> where T : ClaimTransform
    {
        /// <summary>
        /// Collection of claim transformations applied in order.
        /// </summary>
        List<T> ClaimTransforms { get; set; }
    }
}
