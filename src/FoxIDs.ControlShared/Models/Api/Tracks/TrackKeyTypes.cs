using System;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Key storage and rotation strategies for track signing keys.
    /// </summary>
    public enum TrackKeyTypes
    {
        Contained = 0,
        [Obsolete("KeyVault is phased out.")]
        KeyVaultRenewSelfSigned = 1,
        ContainedRenewSelfSigned = 10
    }
}
