using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum SamlMetadataContactPersonTypes
    {
        [EnumMember(Value = "technical")]
        Technical = 10,
        [EnumMember(Value = "support")]
        Support = 20,
        [EnumMember(Value = "administrative")]
        Administrative = 30,
        [EnumMember(Value = "billing")]
        Billing = 40,
        [EnumMember(Value = "other")]
        Other = 50
    }
}
