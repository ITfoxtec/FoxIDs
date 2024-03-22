using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum DynamicElementTypes
    {
        [EnumMember(Value = "email")]
        Email = 5,
        [EnumMember(Value = "email_password")]
        EmailAndPassword = 10,

        [EnumMember(Value = "name")]
        Name = 20,
        [EnumMember(Value = "given_name")]
        GivenName = 21,
        [EnumMember(Value = "family_name")]
        FamilyName = 22
    }
}
