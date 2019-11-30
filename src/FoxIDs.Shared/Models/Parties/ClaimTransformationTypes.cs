using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum ClaimTransformationTypes
    {
        [EnumMember(Value = "constant")]
        Constant = 10,
        [EnumMember(Value = "map")]
        Map = 20,
        [EnumMember(Value = "reg_ex")]
        RegEx = 30,
        [EnumMember(Value = "Concatenate")]
        Concatenate = 40
    }
}
