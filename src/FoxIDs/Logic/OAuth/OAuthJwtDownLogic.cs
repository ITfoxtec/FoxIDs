using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;
using ITfoxtec.Identity.Tokens;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FoxIDs.Models.Session;

namespace FoxIDs.Logic
{
    public class OAuthJwtDownLogic<TParty, TClient, TScope, TClaim> : LogicSequenceBase where TParty : OAuthDownParty<TClient, TScope, TClaim> where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly TrackKeyLogic trackKeyLogic;
        private readonly TrackIssuerLogic trackIssuerLogic;
        private readonly ClaimsOAuthDownLogic<TClient, TScope, TClaim> claimsOAuthDownLogic;
        private readonly OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic;
        private readonly ActiveSessionLogic activeSessionLogic;

        public OAuthJwtDownLogic(TelemetryScopedLogger logger, TrackKeyLogic trackKeyLogic, TrackIssuerLogic trackIssuerLogic, ClaimsOAuthDownLogic<TClient, TScope, TClaim> claimsOAuthDownLogic, OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic, ActiveSessionLogic activeSessionLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.trackKeyLogic = trackKeyLogic;
            this.trackIssuerLogic = trackIssuerLogic;
            this.claimsOAuthDownLogic = claimsOAuthDownLogic;
            this.oauthResourceScopeDownLogic = oauthResourceScopeDownLogic;
            this.activeSessionLogic = activeSessionLogic;
        }

        public async Task<string> CreateAccessTokenAsync(TParty party, string routeUrl, IEnumerable<Claim> claims, IEnumerable<string> selectedScopes, string algorithm, bool saveActiveSession)
        {
            var accessTokenClaims = new List<Claim>();

            (var audiences, var audienceScopes) = await oauthResourceScopeDownLogic.GetValidResourceAsync(party.Client, selectedScopes);

            accessTokenClaims.AddRange(await claimsOAuthDownLogic.FilterJwtClaimsAsync(party.Client, claims, selectedScopes, includeAccessTokenClaims: true));

            var clientClaims = claimsOAuthDownLogic.GetClientJwtClaims(party.Client);
            if (clientClaims?.Count() > 0)
            {
                accessTokenClaims.AddRange(clientClaims);
            }

            if (audiences.Count() > 0 && audienceScopes.Count() > 0)
            {
                accessTokenClaims.AddClaim(JwtClaimTypes.Scope, audienceScopes.ToSpaceList());
            }
            else
            {
                audiences.Add(party.Client.ClientId);
            }

            accessTokenClaims.AddClaim(JwtClaimTypes.ClientId, party.Client.ClientId);

            var adjustedClaims = claimsOAuthDownLogic.AdjustClaims(accessTokenClaims);
            logger.ScopeTrace(() => $"AppReg, JWT access token claims '{adjustedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            if (saveActiveSession)
            {
                await activeSessionLogic.SaveSessionAsync(new DownPartySessionLink { Id = party.Id, Type = party.Type }, adjustedClaims);
            }
            var token = JwtHandler.CreateToken(await trackKeyLogic.GetPrimarySecurityKeyAsync(RouteBinding.Key), trackIssuerLogic.GetIssuer(routeUrl), audiences, adjustedClaims, expiresIn: party.Client.AccessTokenLifetime, algorithm: algorithm, typ: IdentityConstants.JwtHeaders.MediaTypes.AtJwt);
            return await token.ToJwtString();
        }

        public async Task<ClaimsPrincipal> ValidatePartyClientTokenAsync(TClient client, string routeUrl, string token, bool validateLifetime = true)
        {
            var issuerSigningKeys = new List<JsonWebKey>
            {
                RouteBinding.Key.PrimaryKey.Key
            };
            if (RouteBinding.Key.SecondaryKey != null)
            {
                issuerSigningKeys.Add(RouteBinding.Key.SecondaryKey.Key);
            }

            try
            {
                (var claimsPrincipal, _) = await Task.FromResult(JwtHandler.ValidateToken(token, trackIssuerLogic.GetIssuer(routeUrl), issuerSigningKeys, audience: client.ClientId, validateLifetime: validateLifetime));
                return claimsPrincipal;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Application client JWT not valid. Client id '{client.ClientId}', Route '{RouteBinding.Route}'.");
                return null;
            }
        }

        public async Task<ClaimsPrincipal> ValidateTokenAsync(string routeUrl, string token, string audience = null, bool validateLifetime = true)
        {
            var issuerSigningKeys = new List<JsonWebKey>
            {
                RouteBinding.Key.PrimaryKey.Key
            };
            if (RouteBinding.Key.SecondaryKey != null)
            {
                issuerSigningKeys.Add(RouteBinding.Key.SecondaryKey.Key);
            }

            ClaimsPrincipal claimsPrincipal;
            try
            {
                (claimsPrincipal, _) = await Task.FromResult(JwtHandler.ValidateToken(token, trackIssuerLogic.GetIssuer(routeUrl), issuerSigningKeys, audience: audience, validateAudience: !audience.IsNullOrWhiteSpace(), validateLifetime: validateLifetime));
            }
            catch (Exception ex)
            {
                throw new Exception("JWT not valid", ex);
            }

            await activeSessionLogic.ValidateSessionAsync(claimsPrincipal.Claims);

            return claimsPrincipal;
        }

        public async Task<ClaimsPrincipal> ValidateClientAssertionAsync(string clientAssertion, string issuer, List<JsonWebKey> clientKeys, string audience)
        {
            (var claimsPrincipal, _) = await Task.FromResult(JwtHandler.ValidateToken(clientAssertion, issuer, clientKeys, audience));
            return claimsPrincipal;
        }
    }
}
