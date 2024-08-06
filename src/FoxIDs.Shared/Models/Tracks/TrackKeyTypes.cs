using System;
using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum TrackKeyTypes
    {
        [EnumMember(Value = "contained")]
        Contained = 0,
        [Obsolete("KeyVault is phased out.")]
        [EnumMember(Value = "key_vault_renew_self_signed")]
        KeyVaultRenewSelfSigned = 1,
        [EnumMember(Value = "contained_renew_self_signed")]
        ContainedRenewSelfSigned = 10
    }
}
