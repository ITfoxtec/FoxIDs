﻿using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Discovery;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Threading.Tasks;
using UrlCombineLib;

namespace FoxIDs.Logic
{
    public class OidcDiscoveryLogic<TParty, TClient, TScope, TClaim> : LogicBase where TParty : OidcDownParty where TClient : OidcDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;

        public OidcDiscoveryLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
        }

        public async Task<OidcDiscovery> OpenidConfiguration(string partyId)
        {
            logger.SetScopeProperty("downPartyId", partyId);
            var party = await tenantRepository.GetAsync<TParty>(partyId);

            var oidcDiscovery = new OidcDiscovery
            {
                Issuer = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName),
                AuthorizationEndpoint = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName, RouteBinding.PartyNameAndBinding, Constants.Routes.OAuthController, Constants.Endpoints.Authorize),
                TokenEndpoint = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName, RouteBinding.PartyNameAndBinding, Constants.Routes.OAuthController, Constants.Endpoints.Token),
                UserInfoEndpoint = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName, RouteBinding.PartyNameAndBinding, Constants.Routes.OAuthController, Constants.Endpoints.UserInfo),
                EndSessionEndpoint = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName, RouteBinding.PartyNameAndBinding, Constants.Routes.OAuthController, Constants.Endpoints.EndSession),
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

                if(party.Client.EnablePkce == true)
                {
                    oidcDiscovery.CodeChallengeMethodsSupported = new[] { IdentityConstants.CodeChallengeMethods.Plain, IdentityConstants.CodeChallengeMethods.S256 };
                }
            }

            return oidcDiscovery;
        }

        public JsonWebKeySet Keys(string partyId)
        {
            logger.SetScopeProperty("downPartyId", partyId);

            var jonWebKeySet = new JsonWebKeySet();
            jonWebKeySet.Keys.Add(RouteBinding.PrimaryKey.GetPublicKey());
            if (RouteBinding.SecondaryKey != null)
            {
                jonWebKeySet.Keys.Add(RouteBinding.SecondaryKey.GetPublicKey());
            }

            return jonWebKeySet;
        }
    }
}
