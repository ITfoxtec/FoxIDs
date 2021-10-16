using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum ClaimTransformActions
    {
        [EnumMember(Value = "add")]
        Add = 10,
        [EnumMember(Value = "replace")]
        Replace = 20,
        [EnumMember(Value = "remove")]
        Remove = 30,
    }
}