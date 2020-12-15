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
    public class ClaimsLogic<TClient, TScope, TClaim> : LogicBase where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;

        public ClaimsLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public Task<List<Claim>> FilterJwtClaims(TClient client, IEnumerable<Claim> jwtClaims, IEnumerable<string> selectedScopes, bool includeIdTokenClaims = false, bool includeAccessTokenClaims = false)
        {
            if (jwtClaims == null)
            {
                return Task.FromResult(new List<Claim>(jwtClaims));
            }

            var filterClaimTypes = GetFilterJwtClaimTypes(client, selectedScopes, includeIdTokenClaims, includeAccessTokenClaims);
            var filterClaims = jwtClaims.Where(c => filterClaimTypes.Contains(c.Type));

            return Task.FromResult(TruncateJwtClaimValues(filterClaims));            
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
                filterClaimTypes = filterClaimTypes.ConcatOnce(client.Claims?.Cast<OidcDownClaim>().Where(c => c.InIdToken).Select(c => c.Claim));
                filterClaimTypes = filterClaimTypes.ConcatOnce(Constants.DefaultClaims.IdToken);
            }
            if (includeAccessTokenClaims)
            {
                filterClaimTypes = filterClaimTypes.ConcatOnce(client.Claims?.Select(c => c.Claim));
                filterClaimTypes = filterClaimTypes.ConcatOnce(Constants.DefaultClaims.AccessToken);
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

        public Task<List<Claim>> FromJwtToSamlClaims(IEnumerable<Claim> jwtClaims)
        {
            try
            {
                var mappings = GetMappings(RouteBinding);

                var samlClaims = new List<Claim>();

                FromJwtAuthTimeToSaml(samlClaims, jwtClaims);
                FromJwtAmrToSaml(samlClaims, jwtClaims);

                foreach (var jwtClaim in jwtClaims.Where(c => c.Type != JwtClaimTypes.AuthTime && c.Type != JwtClaimTypes.Amr))
                {
                    var claimMap = mappings.FirstOrDefault(m => m.JwtClaim.Equals(jwtClaim.Type, StringComparison.InvariantCultureIgnoreCase));
                    if (claimMap != null)
                    {
                        samlClaims.Add(new Claim(claimMap.SamlClaim, jwtClaim.Value, jwtClaim.ValueType, jwtClaim.Issuer, jwtClaim.OriginalIssuer));
                    }
                    else
                    {
                        samlClaims.Add(jwtClaim);
                    }
                }
                return Task.FromResult(samlClaims);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to map JWT claims to SAML claims.");
                throw;
            }
        }

        private void FromJwtAuthTimeToSaml(List<Claim> samlClaims, IEnumerable<Claim> jwtClaims)
        {
            var jwtClaim = jwtClaims.Where(c => c.Type == JwtClaimTypes.AuthTime).FirstOrDefault();
            if(jwtClaim != null)
            {
                var value = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(jwtClaim.Value)).UtcDateTime.ToString("o");
                samlClaims.Add(new Claim(ClaimTypes.AuthenticationInstant, value, jwtClaim.ValueType, jwtClaim.Issuer, jwtClaim.OriginalIssuer));
            }
        }

        private void FromJwtAmrToSaml(List<Claim> samlClaims, IEnumerable<Claim> jwtClaims)
        {
            var jwtClaimValues = jwtClaims.Where(c => c.Type == JwtClaimTypes.Amr).FirstOrDefault()?.Value.ToSpaceList();

            // TODO, implement mapping
            if (jwtClaimValues.Contains(IdentityConstants.AuthenticationMethodReferenceValues.Pwd))
            {
                samlClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, AuthnContextClassTypes.PasswordProtectedTransport.OriginalString));
            }
        }

        public Task<List<Claim>> FromSamlToJwtClaims(IEnumerable<Claim> samlClaims)
        {
            try
            {
                var mappings = GetMappings(RouteBinding);

                var jwtClaims = new List<Claim>();

                FromSamlAuthTimeToJwt(jwtClaims, samlClaims);
                FromSamlAmrToJwt(jwtClaims, samlClaims);

                foreach (var samlClaim in samlClaims)
                {
                    var claimMap = mappings.FirstOrDefault(m => m.SamlClaim.Equals(samlClaim.Type, StringComparison.InvariantCultureIgnoreCase));
                    if (claimMap != null)
                    {
                        jwtClaims.Add(new Claim(claimMap.JwtClaim, samlClaim.Value, samlClaim.ValueType, samlClaim.Issuer, samlClaim.OriginalIssuer));
                    }
                    else
                    {
                        //TODO validate JWT claim type and value max length

                        jwtClaims.Add(samlClaim);
                    }
                }
                return Task.FromResult(TruncateJwtClaimValues(jwtClaims));

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to map SAML claims to JWT claims.");
                throw;
            }
        }

        private List<Claim> TruncateJwtClaimValues(IEnumerable<Claim> jwtClaims)
        {
            var truncateClaims = new List<Claim>();
            foreach (var claim in jwtClaims)
            {
                if(claim.Value?.Length > Constants.Models.Claim.ValueLength)
                {
                    truncateClaims.AddClaim(claim.Type, claim.Value.Substring(0, Constants.Models.Claim.ValueLength), claim.ValueType, claim.Issuer);
                }
                else
                {
                    truncateClaims.Add(claim);
                }
            }
            return truncateClaims;
        }

        private void FromSamlAuthTimeToJwt(List<Claim> jwtClaims, IEnumerable<Claim> samlClaims)
        {
            var samlClaim = samlClaims.Where(c => c.Type == ClaimTypes.AuthenticationInstant).FirstOrDefault();
            if (samlClaim != null)
            {
                var value = new DateTimeOffset(DateTime.Parse(samlClaim.Value)).ToUnixTimeSeconds();
                jwtClaims.Add(new Claim(JwtClaimTypes.AuthTime, value.ToString(), samlClaim.ValueType, samlClaim.Issuer, samlClaim.OriginalIssuer));
            }
        }

        private void FromSamlAmrToJwt(List<Claim> jwtClaims, IEnumerable<Claim> samlClaims)
        {
            var samlClaimValues = samlClaims.Where(c => c.Type == ClaimTypes.AuthenticationMethod).Select(c => c.Value).ToList();

            if(samlClaimValues?.Count > 0)
            {
                // TODO, implement mapping
                if (samlClaimValues.Any(c => AuthnContextClassTypes.PasswordProtectedTransport.OriginalString.Equals(c, StringComparison.InvariantCultureIgnoreCase) || AuthnContextClassTypes.UserNameAndPassword.OriginalString.Equals(c, StringComparison.InvariantCultureIgnoreCase)))
                {
                    jwtClaims.Add(new Claim(JwtClaimTypes.Amr, IdentityConstants.AuthenticationMethodReferenceValues.Pwd));
                }
            }
        }

        private IEnumerable<ClaimMap> GetMappings(RouteBinding RouteBinding)
        {
            var mappings = GetDefaultMappings();

            if (RouteBinding.ClaimMappings != null && RouteBinding.ClaimMappings.Mappings?.Count() > 0)
            {
                mappings = mappings.ConcatOnce(RouteBinding.ClaimMappings.Mappings, (f, s) => s.JwtClaim == f.JwtClaim);
            }

            mappings = mappings.ConcatOnce(GetDefaultChangeableMappings(), (f, s) => s.JwtClaim == f.JwtClaim);

            return mappings;
        }

        private IEnumerable<ClaimMap> GetDefaultMappings()
        {
            yield return new ClaimMap { JwtClaim = JwtClaimTypes.Subject, SamlClaim = ClaimTypes.NameIdentifier };
            yield return new ClaimMap { JwtClaim = Constants.JwtClaimTypes.SubFormat, SamlClaim = Saml2ClaimTypes.NameIdFormat };
            yield return new ClaimMap { JwtClaim = JwtClaimTypes.SessionId, SamlClaim = Saml2ClaimTypes.SessionIndex };

            yield return new ClaimMap { JwtClaim = JwtClaimTypes.Email, SamlClaim = ClaimTypes.Email };
        }

        private IEnumerable<ClaimMap> GetDefaultChangeableMappings()
        {
            yield return new ClaimMap { JwtClaim = JwtClaimTypes.GivenName, SamlClaim = ClaimTypes.GivenName };
            yield return new ClaimMap { JwtClaim = JwtClaimTypes.FamilyName, SamlClaim = ClaimTypes.Surname };

            yield return new ClaimMap { JwtClaim = JwtClaimTypes.Role, SamlClaim = ClaimTypes.Role };
        }
    }
}
