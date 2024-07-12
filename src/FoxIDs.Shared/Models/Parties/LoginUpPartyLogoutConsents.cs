using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum LoginUpPartyLogoutConsents
    {
        [EnumMember(Value = "always")]
        Always,
        [EnumMember(Value = "if_required")]
        IfRequired,
        [EnumMember(Value = "never")]
        Never
    }
}
