using System;

namespace FoxIDs.Models.Api
{
    public enum TrackKeyTypes
    {
        Contained = 0,
        [Obsolete("KeyVault is phased out.")]
        KeyVaultRenewSelfSigned = 1,
        ContainedRenewSelfSigned = 10
    }
}
