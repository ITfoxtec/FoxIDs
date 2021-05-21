using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    /// <summary>
    /// Comparison methods.
    /// </summary>
    public enum SamlAuthnContextComparisonTypes
    {
        [EnumMember(Value = "exact")]
        Exact = 10,
        [EnumMember(Value = "minimum")]
        Minimum = 20,
        [EnumMember(Value = "maximum")]
        Maximum = 30,
        [EnumMember(Value = "better")]
        Better = 40,
    }
}
