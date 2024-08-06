using System;
using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum ClientKeyTypes
    {
        [EnumMember(Value = "contained")]
        Contained = 0,
        [Obsolete("KeyVault is phased out.")]
        [EnumMember(Value = "key_vault_upload")]
        KeyVaultImport = 20,
    }
}
