using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Claims;
using ITfoxtec.Identity.Saml2.Schemas;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Logic
{
    public class ClaimsOAuthDownLogic<TClient, TScope, TClaim> : ClaimsDownLogic where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;

        public ClaimsOAuthDownLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(logger, httpContextAccessor)
        {
            this.logger = logger;
        }

        public Task<List<Claim>> FilterJwtClaimsAsync(TClient client, IEnumerable<Claim> jwtClaims, IEnumerable<string> selectedScopes, bool includeIdTokenClaims = false, bool includeAccessTokenClaims = false)
        {
            if (!includeIdTokenClaims && !includeAccessTokenClaims)
            {
                throw new ArgumentException($"{nameof(includeIdTokenClaims)} and {nameof(includeAccessTokenClaims)} can not be false at the same time.");
            }

            if (jwtClaims == null)
            {
                return Task.FromResult(new List<Claim>(jwtClaims));
            }

            var filterClaimTypes = GetFilterJwtClaimTypes(client, selectedScopes, includeIdTokenClaims, includeAccessTokenClaims);
            return FilterJwtClaimsAsync(filterClaimTypes, jwtClaims);
        }

        private List<string> GetFilterJwtClaimTypes(TClient client, IEnumerable<string> selectedScopes, bool includeIdTokenClaims, bool includeAccessTokenClaims)
        {
            if(includeIdTokenClaims && !(client is OidcDownClient))
            {
                throw new InvalidOperationException("Include ID Token claims only possible for OIDC Down Client.");
            }

            var filterClaimTypes = new List<string>();

            if (includeIdTokenClaims)
            {
                var acceptAllIdTokenClaims = client.Claims?.Cast<OidcDownClaim>()?.Where(c => c.Claim == "*" && c.InIdToken)?.Count() > 0;
                if (acceptAllIdTokenClaims)
                {
                    filterClaimTypes.Add("*");
                    return filterClaimTypes;
                }

                filterClaimTypes = filterClaimTypes.ConcatOnce(client.Claims?.Cast<OidcDownClaim>().Where(c => c.InIdToken).Select(c => c.Claim));
                filterClaimTypes = filterClaimTypes.ConcatOnce(Constants.DefaultClaims.IdToken);
            }
            if (includeAccessTokenClaims)
            {
                GetFilterClaimTypes(client.Claims?.Cast<OAuthDownClaim>(), filterClaimTypes);
            }

            if (selectedScopes?.Count() > 0)
            {
                if (includeIdTokenClaims)
                {
                    var idTokenVoluntaryClaims = (client as OidcDownClient).Scopes?.Where(s => s.VoluntaryClaims != null && selectedScopes.Any(ss => ss == s.Scope)).SelectMany(s => s.VoluntaryClaims).Where(c => c.InIdToken).Select(c => c.Claim).ToList();
                    filterClaimTypes = filterClaimTypes.ConcatOnce(idTokenVoluntaryClaims);
                }
                if(includeAccessTokenClaims)
                {
                    if(client is OidcDownClient)
                    {
                        var accessTokenVoluntaryClaims = (client as OidcDownClient).Scopes?.Where(s => s.VoluntaryClaims != null && selectedScopes.Any(ss => ss == s.Scope)).SelectMany(s => s.VoluntaryClaims).Select(c => c.Claim).ToList();
                        filterClaimTypes = filterClaimTypes.ConcatOnce(accessTokenVoluntaryClaims);
                    }
                    else
                    {
                        var accessTokenVoluntaryClaims = client.Scopes?.Where(s => s.VoluntaryClaims != null && selectedScopes.Any(ss => ss == s.Scope)).SelectMany(s => s.VoluntaryClaims).Select(c => c.Claim).ToList();
                        filterClaimTypes = filterClaimTypes.ConcatOnce(accessTokenVoluntaryClaims);
                    }
                }
            }

            return filterClaimTypes;
        }

        public List<Claim> GetClientJwtClaims(TClient client, bool onlyIdTokenClaims = false)
        {
            var claims = client.Claims?.Where(c => c.Values?.Count() > 0);

            if (onlyIdTokenClaims)
            {
                claims = claims?.Cast<OidcDownClaim>().Where(c => c.InIdToken).Cast<TClaim>();
            }

            return claims?.SelectMany(item => item.Values.Select(value => new Claim(item.Claim, value))).ToList();
        }
    }
}
