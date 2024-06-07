using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Discovery;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using ITfoxtec.Identity.Util;
using ITfoxtec.Identity.Models;
using System.Collections.Generic;
using System;

namespace FoxIDs.Logic
{
    public class OidcDiscoveryExposeDownLogic<TParty, TClient, TScope, TClaim> : LogicSequenceBase where TParty : OAuthDownParty<TClient, TScope, TClaim> where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly TrackIssuerLogic trackIssuerLogic;

        public OidcDiscoveryExposeDownLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, TrackIssuerLogic trackIssuerLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.trackIssuerLogic = trackIssuerLogic;
        }

        public async Task<OidcDiscovery> OpenidConfigurationAsync(string partyId)
        {
            logger.ScopeTrace(() => "AppReg, OpenID configuration request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = RouteBinding.DownParty != null ? await tenantDataRepository.GetAsync<TParty>(partyId) : null;

            var oidcDiscovery = new OidcDiscovery
            {
                Issuer = trackIssuerLogic.GetIssuer(),
                AuthorizationEndpoint = UrlCombine.Combine(HttpContext.GetHostWithTenantAndTrack(), RouteBinding.PartyNameAndBinding, Constants.Routes.OAuthController, Constants.Endpoints.Authorize),
                TokenEndpoint = UrlCombine.Combine(HttpContext.GetHostWithTenantAndTrack(), RouteBinding.PartyNameAndBinding, Constants.Routes.OAuthController, Constants.Endpoints.Token),
                UserInfoEndpoint = UrlCombine.Combine(HttpContext.GetHostWithTenantAndTrack(), RouteBinding.PartyNameAndBinding, Constants.Routes.OAuthController, Constants.Endpoints.UserInfo),
                EndSessionEndpoint = UrlCombine.Combine(HttpContext.GetHostWithTenantAndTrack(), RouteBinding.PartyNameAndBinding, Constants.Routes.OAuthController, Constants.Endpoints.EndSession),
                JwksUri = UrlCombine.Combine(HttpContext.GetHostWithTenantAndTrack(), RouteBinding.PartyNameAndBinding, IdentityConstants.OidcDiscovery.Path, IdentityConstants.OidcDiscovery.Keys),
                FrontchannelLogoutSupported = true,
                FrontchannelLogoutSessionSupported = true,
                TokenEndpointAuthMethodsSupported = new[] { IdentityConstants.TokenEndpointAuthMethods.ClientSecretPost, IdentityConstants.TokenEndpointAuthMethods.ClientSecretBasic, IdentityConstants.TokenEndpointAuthMethods.PrivateKeyJwt },
                TokenEndpointAuthSigningAlgValuesSupported = new[] { IdentityConstants.Algorithms.Asymmetric.RS256 }
            };

            if (party?.Client != null)
            {
                oidcDiscovery.ResponseModesSupported = new[] { IdentityConstants.ResponseModes.Fragment, IdentityConstants.ResponseModes.Query, IdentityConstants.ResponseModes.FormPost };
                oidcDiscovery.SubjectTypesSupported = new[] { IdentityConstants.SubjectTypes.Public/*, IdentityConstants.SubjectTypes.Pairwise*/ };
                oidcDiscovery.IdTokenSigningAlgValuesSupported = new[] { IdentityConstants.Algorithms.Asymmetric.RS256 };
                oidcDiscovery.ResponseTypesSupported = party.Client.ResponseTypes;
                oidcDiscovery.ScopesSupported = oidcDiscovery.ScopesSupported.ConcatOnce(party.Client.Scopes?.Select(s => s.Scope));
                oidcDiscovery.ClaimsSupported = oidcDiscovery.ClaimsSupported.ConcatOnce(Constants.DefaultClaims.IdToken).ConcatOnce(Constants.DefaultClaims.AccessToken)
                    .ConcatOnce(party.Client.Claims?.Where(c => c.Claim?.Contains('*') != true)?.Select(c => c.Claim).ToList()).ConcatOnce(party.Client.Scopes?.Where(s => s.VoluntaryClaims != null).SelectMany(s => s.VoluntaryClaims?.Select(c => c.Claim)).ToList());

                if(party?.Client.RequirePkce == true)
                {
                    oidcDiscovery.CodeChallengeMethodsSupported = new[] { IdentityConstants.CodeChallengeMethods.Plain, IdentityConstants.CodeChallengeMethods.S256 };
                }
            }
            else
            {
                oidcDiscovery.ResponseModesSupported = new[] { IdentityConstants.ResponseModes.Fragment, IdentityConstants.ResponseModes.Query, IdentityConstants.ResponseModes.FormPost };
                oidcDiscovery.SubjectTypesSupported = new[] { IdentityConstants.SubjectTypes.Public/*, IdentityConstants.SubjectTypes.Pairwise*/ };
                oidcDiscovery.IdTokenSigningAlgValuesSupported = new[] { IdentityConstants.Algorithms.Asymmetric.RS256 };
                oidcDiscovery.ResponseTypesSupported = new[] { IdentityConstants.ResponseTypes.Code };
                oidcDiscovery.ScopesSupported = oidcDiscovery.ScopesSupported;
                oidcDiscovery.ClaimsSupported = oidcDiscovery.ClaimsSupported.ConcatOnce(Constants.DefaultClaims.IdToken).ConcatOnce(Constants.DefaultClaims.AccessToken);
                oidcDiscovery.CodeChallengeMethodsSupported = new[] { IdentityConstants.CodeChallengeMethods.Plain, IdentityConstants.CodeChallengeMethods.S256 };
            }

            return oidcDiscovery;
        }

        public JsonWebKeySet Keys(string partyId)
        {
            logger.ScopeTrace(() => "AppReg, OpenID configuration keys request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);

            var jonWebKeySet = new JsonWebKeySet() { Keys = new List<JsonWebKey>() };
            if (!RouteBinding.Key.PrimaryKey.ExternalKeyIsNotReady)
            {
                jonWebKeySet.Keys.Add(RouteBinding.Key.PrimaryKey.Key.GetPublicKey().AddSignatureUse());
            }
            if (RouteBinding.Key.SecondaryKey != null)
            {
                jonWebKeySet.Keys.Add(RouteBinding.Key.SecondaryKey.Key.GetPublicKey().AddSignatureUse());
            }

            return jonWebKeySet;
        }
    }
}
