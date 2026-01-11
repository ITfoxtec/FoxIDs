using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OidcUserInfoDownLogic<TParty, TClient, TScope, TClaim> : LogicSequenceBase where TParty : OidcDownParty<TClient, TScope, TClaim> where TClient : OidcDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly OidcJwtDownLogic<TParty, TClient, TScope, TClaim> oidcJwtDownLogic;

        public OidcUserInfoDownLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, PlanUsageLogic planUsageLogic, OidcJwtDownLogic<TParty, TClient, TScope, TClaim> oidcJwtDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.planUsageLogic = planUsageLogic;
            this.oidcJwtDownLogic = oidcJwtDownLogic;
        }

        public async Task<IActionResult> UserInfoRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "AppReg, OIDC UserInfo request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantDataRepository.GetAsync<TParty>(partyId);
            logger.SetScopeProperty(Constants.Logs.DownPartyType, party.Type.ToString());

            try
            {
                var claims = await GetAccessTokenClaims(party);

                planUsageLogic.LogTokenRequestEvent(UsageLogTokenTypes.UserInfo);

                if (!claims.Any(c => c.Type == JwtClaimTypes.Subject))
                {
                    throw new OAuthRequestException($"Require {JwtClaimTypes.Subject} claim in access token.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                }

                logger.SetUserScopeProperty(claims);

                return new JsonResult(ToClaimsResult(claims));
            }
            catch (OAuthRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new OAuthRequestException(ex.Message, ex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
            }
        }

        private Dictionary<string, object> ToClaimsResult(IEnumerable<Claim> claims)
        {
            var claimsResult = new Dictionary<string, object>();
            if (claims?.Count() > 0)
            {
                var claimGroups = claims.GroupBy(c => c.Type).Select(cg => new { cg.Key, Values = claims.Where(c => c.Type == cg.Key).Select(c => c.Value) });
                foreach (var item in claimGroups)
                {
                    if (item.Values.Count() == 1)
                    {
                        claimsResult.Add(item.Key, item.Values.First());
                    }
                    else
                    {
                        claimsResult.Add(item.Key, item.Values);
                    }
                }
            }
            return claimsResult;
        }

        private async Task<IEnumerable<Claim>> GetAccessTokenClaims(TParty party)
        {
            var accessToken = HttpContext.Request.Headers.GetAuthorizationHeaderBearer();
            if (accessToken.IsNullOrWhiteSpace())
            {
                throw new OAuthRequestException("The access token is not found in the Bearer header.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
            }
            logger.ScopeTrace(() => $"Access token '{accessToken}'.");

            try
            {
                var claimsPrincipal = await oidcJwtDownLogic.ValidateTokenAsync(party.UsePartyIssuer ? RouteBinding.RouteUrl : null, accessToken);
                return claimsPrincipal.Claims;
            }
            catch (SessionException sex)
            {
                throw new OAuthRequestException("The access token session is missing or no longer valid.", sex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
            }
            catch (Exception ex)
            {
                throw new OAuthRequestException("The access token is not valid.", ex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
            }
        }
    }
}
