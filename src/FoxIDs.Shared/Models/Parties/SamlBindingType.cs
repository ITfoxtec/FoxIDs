using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum SamlBindingType
    {
        [EnumMember(Value = "redirect")]
        Redirect,
        [EnumMember(Value = "post")]
        Post
    }
}
