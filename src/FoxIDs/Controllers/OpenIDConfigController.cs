using ITfoxtec.Identity;
using ITfoxtec.Identity.Discovery;
using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using UrlCombineLib;
using Microsoft.IdentityModel.Tokens;
using FoxIDs.Infrastructure.Filters;

namespace FoxIDs.Controllers
{
    [CorsPolicy]
    public class OpenIDConfigController : EndpointController
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;

        public OpenIDConfigController(FoxIDsSettings settings, TelemetryScopedLogger logger, ITenantRepository tenantRepository) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.tenantRepository = tenantRepository;
        }

        public async Task<IActionResult> OpenidConfiguration()
        {
            try
            {
                logger.ScopeTrace($"Openid configuration, Down type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyType.OAuth2:
                        return Json(await OpenidConfiguration<OAuthDownParty, OAuthDownClient, OAuthDownScope, OAuthDownClaim>(), JsonExtensions.SettingsIndented);

                    case PartyType.Oidc:
                        return Json(await OpenidConfiguration<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>(), JsonExtensions.SettingsIndented);

                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Request failed for client id '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<OidcDiscovery> OpenidConfiguration<TParty, TClient, TScope, TClaim>() where TParty : OAuthDownParty<TClient, TScope, TClaim> where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
       // private async Task<OidcDiscovery> OpenidConfiguration<TParty<TClient, TScope, TClaim>, TClient, TScope, TClaim>() where TParty : OAuthDownParty<TClient, TScope, TClaim> where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope where TClaim : OAuthDownClaim
        {
            logger.SetScopeProperty("downPartyId", RouteBinding.DownParty.Id);
            var party = await tenantRepository.GetAsync<TParty>(RouteBinding.DownParty.Id);

            var oidcDiscovery = new OidcDiscovery
            {
                Issuer = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName),
                AuthorizationEndpoint = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName, RouteBinding.PartyNameAndBinding, Constants.Routes.OAuthController, IdentityConstants.Endpoints.Authorization),
                TokenEndpoint = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName, RouteBinding.PartyNameAndBinding, Constants.Routes.OAuthController, IdentityConstants.Endpoints.Token),
                EndSessionEndpoint = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName, RouteBinding.PartyNameAndBinding, Constants.Routes.OAuthController, IdentityConstants.Endpoints.EndSession),
                JwksUri = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName, RouteBinding.PartyNameAndBinding, IdentityConstants.OidcDiscovery.Path, IdentityConstants.OidcDiscovery.Keys),
            };

            if (party.Client != null)
            {
                oidcDiscovery.ResponseModesSupported = new[] { IdentityConstants.ResponseModes.Fragment, IdentityConstants.ResponseModes.Query, IdentityConstants.ResponseModes.FormPost };
                oidcDiscovery.SubjectTypesSupported = new[] { IdentityConstants.SubjectTypes.Pairwise };
                oidcDiscovery.IdTokenSigningAlgValuesSupported = new[] { IdentityConstants.Algorithms.Asymmetric.RS256 };
                oidcDiscovery.ResponseTypesSupported = party.Client.ResponseTypes;
                oidcDiscovery.ScopesSupported = oidcDiscovery.ScopesSupported.ConcatOnce(party.Client.Scopes?.Select(s => s.Scope));
                oidcDiscovery.ClaimsSupported = oidcDiscovery.ClaimsSupported.ConcatOnce(Constants.DefaultClaims.IdToken).ConcatOnce(Constants.DefaultClaims.AccessToken)
                    .ConcatOnce(party.Client.Claims?.Select(c => c.Claim).ToList()).ConcatOnce(party.Client.Scopes?.Where(s => s.VoluntaryClaims != null).SelectMany(s => s.VoluntaryClaims?.Select(c => c.Claim)).ToList());
            }

            return oidcDiscovery;
        }

        public IActionResult Keys()
        {
            try
            {
                logger.ScopeTrace($"Openid configuration keys, Down type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyType.OAuth2:
                    case PartyType.Oidc:
                        logger.SetScopeProperty("downPartyId", RouteBinding.DownParty.Id);

                        var jonWebKeySet = new JsonWebKeySet();
                        jonWebKeySet.Keys.Add(RouteBinding.PrimaryKey.GetPublicKey());
                        if (RouteBinding.SecondaryKey != null)
                        {
                            jonWebKeySet.Keys.Add(RouteBinding.SecondaryKey.GetPublicKey());
                        }

                        return Json(jonWebKeySet, JsonExtensions.SettingsIndented);

                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Request failed for client id '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}
