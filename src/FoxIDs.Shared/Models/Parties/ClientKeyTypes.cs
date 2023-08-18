using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum ClientKeyTypes
    {
        [EnumMember(Value = "key_vault_self_signed")]
        KeyVaultSelfSigned = 10,
        [EnumMember(Value = "key_vault_upload")]
        KeyVaultImport = 20,
    }
}
