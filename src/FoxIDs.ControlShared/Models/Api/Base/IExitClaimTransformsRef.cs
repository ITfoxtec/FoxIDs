using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Indicates the message supports claim transforms executed on exit.
    /// </summary>
    public interface IExitClaimTransformsRef<T> where T : ClaimTransform
    {
        /// <summary>
        /// Claim transforms applied when leaving the party.
        /// </summary>
        List<T> ExitClaimTransforms { get; set; }
    }
}
