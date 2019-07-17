using System;
using System.Linq;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Security.Claims;
using ITfoxtec.Identity.Saml2.Claims;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;

namespace FoxIDs.Logic
{
    public class OidcEndSessionDownLogic<TParty, TClient, TScope, TClaim> : LogicBase where TParty : OidcDownParty<TClient, TScope, TClaim> where TClient : OidcDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly JwtLogic<TClient, TScope, TClaim> jwtLogic;
        private readonly OAuthRefreshTokenGrantLogic<TClient, TScope, TClaim> oauthRefreshTokenGrantLogic;

        public OidcEndSessionDownLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, JwtLogic<TClient, TScope, TClaim> jwtLogic, OAuthRefreshTokenGrantLogic<TClient, TScope, TClaim> oauthRefreshTokenGrantLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.jwtLogic = jwtLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
        }

        public async Task<IActionResult> EndSessionRequestAsync(string partyId)
        {
            logger.ScopeTrace("Down, End session request.");
            logger.SetScopeProperty("downPartyId", partyId);
            var party = await tenantRepository.GetAsync<TParty>(partyId);
            if (party.Client == null)
            {
                throw new NotSupportedException($"Party Client not configured.");
            }

            var queryDictionary = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value).ToDictionary();
            var endSessionRequest = queryDictionary.ToObject<EndSessionRequest>();

            logger.ScopeTrace($"end session request '{endSessionRequest.ToJsonIndented()}'.");
            logger.SetScopeProperty("clientId", party.Client.ClientId);

            ValidateEndSessionRequest( party.Client, endSessionRequest);
            logger.ScopeTrace("Down, OIDC End session request accepted.", triggerEvent: true);

            (var validIdToken, var sessionId, var idTokenClaims) = await ValidateIdTokenHintAsync(party.Client, endSessionRequest.IdTokenHint);
            if (!validIdToken)
            {
                if (!endSessionRequest.IdTokenHint.IsNullOrEmpty())
                {
                    throw new OAuthRequestException($"Invalid ID Token hint.") { RouteBinding = RouteBinding };
                }
                else if (party.Client.RequireLogoutIdTokenHint)
                {
                    throw new OAuthRequestException($"ID Token hint is required.") { RouteBinding = RouteBinding };
                }
            }
            if (validIdToken) logger.ScopeTrace("Valid ID token hint.");

            await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantAsync(party.Client, sessionId);

            await sequenceLogic.SaveSequenceDataAsync(new OidcDownSequenceData
            {
                RedirectUri = endSessionRequest.PostLogoutRedirectUri,
                State = endSessionRequest.State,
            });

            var type = RouteBinding.ToUpParties.First().Type.ToEnum<PartyType>();
            logger.ScopeTrace($"Request, Up type '{type}'.");
            switch (type)
            {
                case PartyType.Login:
                    var logoutRequest = new LogoutRequest
                    {
                        DownParty = party,
                        SessionId = sessionId,
                        RequireLogoutConsent = !validIdToken,
                        PostLogoutRedirect = !endSessionRequest.PostLogoutRedirectUri.IsNullOrWhiteSpace(),
                    };
                    return await serviceProvider.GetService<LogoutUpLogic>().LogoutRedirect(RouteBinding.ToUpParties.First(), logoutRequest);
                case PartyType.OAuth2:
                    throw new NotImplementedException();
                case PartyType.Oidc:
                    throw new NotImplementedException();
                case PartyType.Saml2:
                    if (!validIdToken)
                    {
                        throw new OAuthRequestException($"ID Token hint is required for SAML 2.0 Up Party.") { RouteBinding = RouteBinding };
                    }
                    return await serviceProvider.GetService<SamlLogoutUpLogic>().LogoutAsync(RouteBinding.ToUpParties.First().Id, GetSamlUpLogoutRequest( party, sessionId, idTokenClaims));

                default:
                    throw new NotSupportedException($"Party type '{type}' not supported.");
            }
        }

        private LogoutRequest GetSamlUpLogoutRequest(Party party, string sessionId, IEnumerable<Claim> idTokenClaims)
        {
            var samlClaims = new List<Claim>();
            var nameIdClaim = idTokenClaims.FirstOrDefault(c => c.Type == JwtClaimTypes.Subject);
            if(nameIdClaim == null)
            {
                throw new OAuthRequestException($"Requere '{JwtClaimTypes.Subject}' claim in ID Token hint.") { RouteBinding = RouteBinding };
            }
            samlClaims.AddClaim(Saml2ClaimTypes.NameId, nameIdClaim.Value);

            var subFormatClaim = idTokenClaims.FirstOrDefault(c => c.Type == Constants.JwtClaimTypes.SubFormat);
            if (subFormatClaim == null)
            {
                throw new OAuthRequestException($"Requere '{Constants.JwtClaimTypes.SubFormat}' claim in ID Token hint.") { RouteBinding = RouteBinding };
            }
            samlClaims.AddClaim(Saml2ClaimTypes.NameIdFormat, subFormatClaim.Value);

            return new LogoutRequest
            {
                DownParty = party,
                SessionId = sessionId,
                RequireLogoutConsent = false,
                PostLogoutRedirect = true,
                Claims = samlClaims,
            };
        }

        private async Task<(bool, string, IEnumerable<Claim>)> ValidateIdTokenHintAsync(TClient client, string idToken)
        {
            if (!idToken.IsNullOrEmpty())
            {
                var claimsPrincipal = await jwtLogic.ValidatePartyClientTokenAsync(client, idToken, validateLifetime: false);
                if (claimsPrincipal != null)
                {
                    return (true, claimsPrincipal.FindFirstValue(JwtClaimTypes.SessionId), claimsPrincipal.Claims);
                }
            }
            return (false, null, null);
        }

        private void ValidateEndSessionRequest(TClient client, EndSessionRequest endSessionRequest)
        {
            endSessionRequest.Validate();

            if (!endSessionRequest.PostLogoutRedirectUri.IsNullOrWhiteSpace() && !client.RedirectUris.Any(u => u.Equals(endSessionRequest.PostLogoutRedirectUri, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new OAuthRequestException($"Invalid post logout redirect uri '{endSessionRequest.PostLogoutRedirectUri}'.");
            }
        }

        public async Task<IActionResult> EndSessionResponseAsync(string partyId)
        {
            logger.ScopeTrace("Down, End session response.");
            logger.SetScopeProperty("downPartyId", partyId);

            var sequenceData = await sequenceLogic.GetSequenceDataAsync<OidcDownSequenceData>(false);

            var endSessionResponse = new EndSessionResponse
            {
                State = sequenceData.State,
            };

            logger.ScopeTrace($"End session response '{endSessionResponse.ToJsonIndented()}'.");
            var nameValueCollection = endSessionResponse.ToDictionary();

            logger.ScopeTrace($"Redirect Uri '{sequenceData.RedirectUri}'.");
            logger.ScopeTrace("Down, OIDC End session response.", triggerEvent: true);

            await sequenceLogic.RemoveSequenceDataAsync<OidcDownSequenceData>();
            return await nameValueCollection.ToRedirectResultAsync(sequenceData.RedirectUri);
        }
    }
}
