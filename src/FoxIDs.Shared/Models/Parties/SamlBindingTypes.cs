using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum SamlBindingTypes
    {
        [EnumMember(Value = "redirect")]
        Redirect,
        [EnumMember(Value = "post")]
        Post
    }
}
