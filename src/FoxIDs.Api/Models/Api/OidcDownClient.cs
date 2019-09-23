using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OidcDownClient
    {
        [Length(Constants.Models.OAuthParty.Client.ResourceScopesMin, Constants.Models.OAuthParty.Client.ResourceScopesMax)]
        public List<OAuthDownResourceScope> ResourceScopes { get; set; }

        [Length(Constants.Models.OAuthParty.Client.ScopesMin, Constants.Models.OAuthParty.Client.ScopesMax)]
        public List<OidcDownScope> Scopes { get; set; }

        [Length(Constants.Models.OAuthParty.Client.ClaimsMin, Constants.Models.OAuthParty.Client.ClaimsMax)]
        public List<OidcDownClaim> Claims { get; set; }

        [Length(Constants.Models.OAuthParty.Client.ResponseTypesMin, Constants.Models.OAuthParty.Client.ResponseTypesMax, Constants.Models.OAuthParty.Client.ResponseTypeLength)]
        public List<string> ResponseTypes { get; set; }

        [Length(Constants.Models.OAuthParty.Client.RedirectUrisMin, Constants.Models.OAuthParty.Client.RedirectUrisMax, Constants.Models.OAuthParty.Client.RedirectUriLength)]
        public List<string> RedirectUris { get; set; }

        [Range(Constants.Models.OAuthParty.Client.AuthorizationCodeLifetimeMin, Constants.Models.OAuthParty.Client.AuthorizationCodeLifetimeMax)]
        public int? AuthorizationCodeLifetime { get; set; }

        [Range(Constants.Models.OAuthParty.Client.AccessTokenLifetimeMin, Constants.Models.OAuthParty.Client.AccessTokenLifetimeMax)]
        public int AccessTokenLifetime { get; set; }

        [Range(Constants.Models.OAuthParty.Client.RefreshTokenLifetimeMin, Constants.Models.OAuthParty.Client.RefreshTokenLifetimeMax)]
        public int? RefreshTokenLifetime { get; set; }

        [Range(Constants.Models.OAuthParty.Client.RefreshTokenAbsoluteLifetimeMin, Constants.Models.OAuthParty.Client.RefreshTokenAbsoluteLifetimeMax)]
        public int? RefreshTokenAbsoluteLifetime { get; set; }

        public bool? RefreshTokenUseOneTime { get; set; }

        public bool? RefreshTokenLifetimeUnlimited { get; set; }

        [Range(Constants.Models.OidcParty.Client.IdTokenLifetimeMin, Constants.Models.OidcParty.Client.IdTokenLifetimeMax)]
        public int IdTokenLifetime { get; set; }

        public bool? RequireLogoutIdTokenHint { get; set; } = true;
    }
}
