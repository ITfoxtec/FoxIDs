using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OidcUpClientProfileViewModel : IClientAdditionalParameters
    {
        [ListLength(Constants.Models.OAuthUpParty.Client.ScopesMin, Constants.Models.OAuthUpParty.Client.ScopesMax, Constants.Models.OAuthUpParty.ScopeLength, Constants.Models.OAuthUpParty.ScopeRegExPattern)]
        [Display(Name = "Scopes")]
        public List<string> Scopes { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.Client.AdditionalParametersMin, Constants.Models.OAuthUpParty.Client.AdditionalParametersMax)]
        [Display(Name = "Additional parameters")]
        public List<OAuthAdditionalParameter> AdditionalParameters { get; set; } = new List<OAuthAdditionalParameter>();
    }
}
