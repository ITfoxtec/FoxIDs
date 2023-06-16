using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OAuthAdditionalParameter
    {
        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.AdditionalParameterNameLength)]
        [RegularExpression(Constants.Models.OAuthUpParty.Client.AdditionalParameterNameRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.AdditionalParameterValueLength)]
        [RegularExpression(Constants.Models.OAuthUpParty.Client.AdditionalParameterValueRegExPattern)]
        public string Value { get; set; }
    }
}
