using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum ClientKeyTypes
    {
        [EnumMember(Value = "key_vault_renew_self_signed")]
        KeyVaultRenewSelfSigned = 10,
        [EnumMember(Value = "key_vault_upload")]
        KeyVaultUpload = 20,
    }
}
