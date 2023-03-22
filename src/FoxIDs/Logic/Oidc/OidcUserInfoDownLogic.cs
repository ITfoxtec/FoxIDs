using System;
using System.Linq;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;

namespace FoxIDs.Logic
{
    public class OidcUserInfoDownLogic<TParty, TClient, TScope, TClaim> : LogicSequenceBase where TParty : OidcDownParty where TClient : OidcDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly JwtDownLogic<TClient, TScope, TClaim> jwtDownLogic;

        public OidcUserInfoDownLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, JwtDownLogic<TClient, TScope, TClaim> jwtDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.jwtDownLogic = jwtDownLogic;
        }

        public async Task<IActionResult> UserInfoRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "Down, OIDC UserInfo request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantRepository.GetAsync<TParty>(partyId);

            try
            {
                var claims = await GetAccessTokenClaims();

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
            if(claims?.Count() > 0)
            {
                var claimGroups = claims.GroupBy(c => c.Type).Select(cg => new { cg.Key, Values = claims.Where(c => c.Type == cg.Key).Select(c => c.Value) });
                foreach(var item in claimGroups)
                {
                    if(item.Values.Count() == 1)
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

        private async Task<IEnumerable<Claim>> GetAccessTokenClaims()
        {
            try
            {
                var accessToken = HttpContext.Request.Headers.GetAuthorizationHeaderBearer();
                logger.ScopeTrace(() => $"Access token '{accessToken}'.");

                var claimsPrincipal = await jwtDownLogic.ValidateTokenAsync(accessToken);
                if (claimsPrincipal == null)
                {
                    throw new Exception("Access token not valid.");
                }
                return claimsPrincipal.Claims;
            }
            catch (Exception ex)
            {
                throw new OAuthRequestException(ex.Message, ex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
            }
        }
    }
}
