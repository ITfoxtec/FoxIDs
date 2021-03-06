using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum PartyBindingPatterns
    {
        [EnumMember(Value = "brackets")]
        Brackets = 10,
        [EnumMember(Value = "tildes")]
        Tildes = 20,
    }
}
