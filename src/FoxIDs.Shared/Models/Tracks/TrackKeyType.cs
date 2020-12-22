using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum TrackKeyType
    {
        [EnumMember(Value = "contained")]
        Contained,
        [EnumMember(Value = "key_vault_renew_self_signed")]
        KeyVaultRenewSelfSigned,
        [EnumMember(Value = "key_vault_upload")]
        KeyVaultUpload,
    }
}
