using System;
using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum ClaimTransformActions
    {
        [EnumMember(Value = "add")]
        Add = 10,
        [EnumMember(Value = "add_if_not")]
        AddIfNot = 12,
        [Obsolete("backwards compatibility to support spelling error 'add_if_mot'.")]
        [EnumMember(Value = "add_if_mot")]
        AddIfNotObsolete = 13,
        [EnumMember(Value = "add_if_not_out")]
        AddIfNotOut = 15,
        [EnumMember(Value = "replace")]
        Replace = 20,
        [EnumMember(Value = "replace_if_not")]
        ReplaceIfNot = 22,
        [Obsolete("backwards compatibility to support spelling error 'replace_if_mot'.")]
        [EnumMember(Value = "replace_if_mot")]
        ReplaceIfNotObsolete = 23,
        [EnumMember(Value = "remove")]
        Remove = 30,
    }
}