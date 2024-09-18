using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class ExternalLoginUpPartyProfile : UpPartyProfile
    {
        /// <summary>
        /// Add additional parameter or change parameter values.
        /// </summary>
        [ListLength(Constants.Models.OAuthUpParty.Client.AdditionalParametersMin, Constants.Models.OAuthUpParty.Client.AdditionalParametersMax)]
        [JsonProperty(PropertyName = "additional_parameter")]
        public List<OAuthAdditionalParameter> AdditionalParameters { get; set; }
    }
}
