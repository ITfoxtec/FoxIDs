using System;

namespace FoxIDs.Models.Api
{
    public enum ClientKeyTypes
    {
        Contained = 0,
        [Obsolete("KeyVault is phased out.")]
        KeyVaultImport = 20,
    }
}
