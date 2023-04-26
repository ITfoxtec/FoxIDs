using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OAuthAdditionalParameter
    {
        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.AdditionalParameterNameLength)]
        [RegularExpression(Constants.Models.OAuthUpParty.Client.AdditionalParameterNameRegExPattern)]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.AdditionalParameterValueLength)]
        [RegularExpression(Constants.Models.OAuthUpParty.Client.AdditionalParameterValueRegExPattern)]
        [Display(Name = "Value")]
        public string Value { get; set; }
    }
}
