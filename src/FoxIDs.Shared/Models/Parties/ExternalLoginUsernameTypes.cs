using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum ExternalLoginUsernameTypes
    {
        [EnumMember(Value = "email")]
        Email = 100,
        [EnumMember(Value = "text")]
        Text = 200
    }
}
