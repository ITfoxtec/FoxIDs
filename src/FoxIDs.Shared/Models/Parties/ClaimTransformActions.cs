using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum ClaimTransformActions
    {
        [EnumMember(Value = "add")]
        Add = 10,
        [EnumMember(Value = "add_if_mot")]
        AddIfNot = 12,
        [EnumMember(Value = "replace")]
        Replace = 20,
        [EnumMember(Value = "replace_if_mot")]
        ReplaceIfNot = 22,
        [EnumMember(Value = "remove")]
        Remove = 30,
    }
}