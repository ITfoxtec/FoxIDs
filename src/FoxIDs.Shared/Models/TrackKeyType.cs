using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum TrackKeyType
    {
        [EnumMember(Value = "contained")]
        Contained,
        [EnumMember(Value = "key_vault")]
        KeyVault
    }
}
