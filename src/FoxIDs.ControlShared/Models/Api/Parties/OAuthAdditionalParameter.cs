using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Additional parameter forwarded to upstream OAuth/OIDC requests.
    /// </summary>
    public class OAuthAdditionalParameter
    {
        /// <summary>
        /// Parameter name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.AdditionalParameterNameLength)]
        [RegularExpression(Constants.Models.OAuthUpParty.Client.AdditionalParameterNameRegExPattern)]
        [Display(Name = "Name")]
        public string Name { get; set; }

        /// <summary>
        /// Parameter value.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.AdditionalParameterValueLength)]
        [RegularExpression(Constants.Models.OAuthUpParty.Client.AdditionalParameterValueRegExPattern)]
        [Display(Name = "Value")]
        public string Value { get; set; }
    }
}
