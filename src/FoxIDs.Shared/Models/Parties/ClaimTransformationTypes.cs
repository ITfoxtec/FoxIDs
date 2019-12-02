using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum ClaimTransformationTypes
    {
        [EnumMember(Value = "constant")]
        Constant = 10,
        [EnumMember(Value = "match")]
        Match = 20,
        [EnumMember(Value = "reg_ex_match")]
        RegexMatch = 25,
        [EnumMember(Value = "map")]
        Map = 30,
        [EnumMember(Value = "reg_ex_map")]
        RegexMap = 35,
        [EnumMember(Value = "Concatenate")]
        Concatenate = 40
    }
}
