using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum LoginUpPartyLogoutConsent
    {
        [EnumMember(Value = "always")]
        Always,
        [EnumMember(Value = "if_requered")]
        IfRequered,
        [EnumMember(Value = "never")]
        Never
    }
}
