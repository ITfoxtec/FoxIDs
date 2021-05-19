using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum SamlBindingTypes
    {
        [EnumMember(Value = "redirect")]
        Redirect = 10,
        [EnumMember(Value = "post")]
        Post = 20
    }
}
