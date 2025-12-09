using System;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Key storage approaches available for client credentials.
    /// </summary>
    public enum ClientKeyTypes
    {
        Contained = 0,
        [Obsolete("KeyVault is phased out.")]
        KeyVaultImport = 20,
    }
}
