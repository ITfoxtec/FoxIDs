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
    public class ClaimsDownLogic : LogicSequenceBase 
    {
        private readonly TelemetryScopedLogger logger;

        public ClaimsDownLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public Task<List<Claim>> FilterJwtClaimsAsync(List<string> filterClaimTypes, IEnumerable<Claim> claims)
        {
            if (claims == null)
            {
                return Task.FromResult(new List<Claim>(claims));
            }

            var acceptAllClaims = filterClaimTypes.Where(c => c == "*").Count() > 0;
            if (acceptAllClaims)
            {
                return Task.FromResult(TruncateJwtClaimValues(claims));
            }
            else
            {
                var filteredClaims = claims.Where(c => filterClaimTypes.Contains(c.Type));
                return Task.FromResult(TruncateJwtClaimValues(filteredClaims));
            }
        }

        public List<string> GetFilterClaimTypes(IEnumerable<OAuthDownClaim> filterClaims, List<string> filterClaimTypes = null)
        {
            filterClaimTypes = filterClaimTypes ?? new List<string>();

            var acceptAllClaims = filterClaims?.Where(c => c.Claim == "*")?.Count() > 0;
            if (acceptAllClaims)
            {
                filterClaimTypes.Add("*");
            }
            else
            {
                filterClaimTypes = filterClaimTypes.ConcatOnce(filterClaims?.Select(c => c.Claim));
                filterClaimTypes = filterClaimTypes.ConcatOnce(Constants.DefaultClaims.AccessToken);
            }

            return filterClaimTypes;
        }

        public List<Claim> GetClientJwtClaims(IEnumerable<OAuthDownClaim> claims)
        {
            claims = claims?.Where(c => c.Values?.Count() > 0);
            return claims?.SelectMany(item => item.Values.Select(value => new Claim(item.Claim, value))).ToList();
        }

        public Task<List<Claim>> FromJwtToSamlClaimsAsync(IEnumerable<Claim> jwtClaims)
        {
            try
            {
                var mappings = GetMappings(RouteBinding);

                var samlClaims = new List<Claim>();

                FromJwtAuthTimeToSaml(samlClaims, jwtClaims);
                FromJwtAmrToSaml(samlClaims, jwtClaims);

                foreach (var jwtClaim in jwtClaims.Where(c => c.Type != JwtClaimTypes.AuthTime))
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
                if (!samlClaims.Where(c => c.Type == Saml2ClaimTypes.NameIdFormat).Any())
                {
                    samlClaims.AddClaim(Saml2ClaimTypes.NameIdFormat, NameIdentifierFormats.Persistent.OriginalString);
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

            var authTime = jwtClaim == null ? DateTimeOffset.UtcNow : DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(jwtClaim.Value));
            var authTimeValue = authTime.UtcDateTime.ToString("o");
            if(jwtClaim == null)
            {
                samlClaims.Add(new Claim(ClaimTypes.AuthenticationInstant, authTimeValue));
            }
            else
            {
                samlClaims.Add(new Claim(ClaimTypes.AuthenticationInstant, authTimeValue, jwtClaim.ValueType, jwtClaim.Issuer, jwtClaim.OriginalIssuer));
            }
        }

        private void FromJwtAmrToSaml(List<Claim> samlClaims, IEnumerable<Claim> jwtClaims)
        {
            var jwtClaimValues = jwtClaims.Where(c => c.Type == JwtClaimTypes.Amr).Select(c => c.Value).ToList();

            if (jwtClaimValues?.Contains(IdentityConstants.AuthenticationMethodReferenceValues.Mfa) == true)
            {
                samlClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, Constants.Saml.AuthnContextClassTypes.Mfa));
            }
            else if (jwtClaimValues?.Contains(IdentityConstants.AuthenticationMethodReferenceValues.Pwd) == true)
            {
                samlClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, AuthnContextClassTypes.PasswordProtectedTransport.OriginalString));
            }
            else
            {
                samlClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, AuthnContextClassTypes.Unspecified.OriginalString));
            }
        }

        public Task<List<Claim>> FromSamlToJwtClaimsAsync(IEnumerable<Claim> samlClaims)
        {
            try
            {
                var mappings = GetMappings(RouteBinding);

                var jwtClaims = new List<Claim>();

                FromSamlAuthTimeToJwt(jwtClaims, samlClaims);
                FromSamlAmrToJwt(jwtClaims, samlClaims);

                foreach (var samlClaim in samlClaims)
                {
                    var claimMaps = mappings.Where(m => m.SamlClaim.Equals(samlClaim.Type, StringComparison.InvariantCultureIgnoreCase));
                    if (claimMaps?.Count() > 0)
                    {
                        foreach(var claimMap in claimMaps)
                        {
                            jwtClaims.Add(new Claim(claimMap.JwtClaim, samlClaim.Value, samlClaim.ValueType, samlClaim.Issuer, samlClaim.OriginalIssuer));
                        }
                    }
                    else if(!MappedClaimType(samlClaim.Type))
                    {
                        var jwtClaim = new Claim(
                            samlClaim.Type?.Length > Constants.Models.Claim.JwtTypeLength ? samlClaim.Type.Substring(0, Constants.Models.Claim.JwtTypeLength) : samlClaim.Type,
                            samlClaim.Value, samlClaim.ValueType, samlClaim.Issuer, samlClaim.OriginalIssuer);

                        jwtClaims.Add(jwtClaim);
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

        private bool MappedClaimType(string type)
        {
            return type switch
            {
                ClaimTypes.AuthenticationInstant => true,
                ClaimTypes.AuthenticationMethod => true,
                _ => false
            };

        }

        private List<Claim> TruncateJwtClaimValues(IEnumerable<Claim> jwtClaims)
        {
            var truncateClaims = new List<Claim>();
            foreach (var claim in jwtClaims)
            {
                if (Constants.EmbeddedJwtToken.JwtTokenClaims.Contains(claim.Type))
                {
                    if (claim.Value?.Length > Constants.EmbeddedJwtToken.ValueLength)
                    {
                        truncateClaims.AddClaim(claim.Type, claim.Value.Substring(0, Constants.EmbeddedJwtToken.ValueLength), claim.ValueType, claim.Issuer);
                    }
                    else
                    {
                        truncateClaims.Add(claim);
                    }
                }
                else
                {
                    if (claim.Value?.Length > Constants.Models.Claim.ValueLength)
                    {
                        truncateClaims.AddClaim(claim.Type, claim.Value.Substring(0, Constants.Models.Claim.ValueLength), claim.ValueType, claim.Issuer);
                    }
                    else
                    {
                        truncateClaims.Add(claim);
                    }
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
            if(samlClaims.Where(c => c.Type == Constants.SamlClaimTypes.Amr).Any())
            {
                return;
            }

            var samlClaimValues = samlClaims.Where(c => c.Type == ClaimTypes.AuthenticationMethod).Select(c => c.Value).ToList();

            if(samlClaimValues?.Count > 0)
            {
                if (samlClaimValues.Any(c => AuthnContextClassTypes.PasswordProtectedTransport.OriginalString.Equals(c, StringComparison.InvariantCultureIgnoreCase) || AuthnContextClassTypes.UserNameAndPassword.OriginalString.Equals(c, StringComparison.InvariantCultureIgnoreCase)))
                {
                    jwtClaims.Add(new Claim(JwtClaimTypes.Amr, IdentityConstants.AuthenticationMethodReferenceValues.Pwd));
                }
                else if (samlClaimValues.Any(c => Constants.Saml.AuthnContextClassTypes.Mfa.Equals(c, StringComparison.InvariantCultureIgnoreCase)))
                {
                    jwtClaims.Add(new Claim(JwtClaimTypes.Amr, IdentityConstants.AuthenticationMethodReferenceValues.Mfa));
                }
            }
        }

        private IEnumerable<ClaimMap> GetMappings(RouteBinding RouteBinding)
        {
            var mappings = Constants.DefaultClaimMappings.LockedMappings.Select(cm => new ClaimMap { JwtClaim = cm.JwtClaim, SamlClaim = cm.SamlClaim });

            if (RouteBinding.ClaimMappings != null && RouteBinding.ClaimMappings?.Count() > 0)
            {
                mappings = mappings.ConcatOnce(RouteBinding.ClaimMappings, (f, s) => s.JwtClaim == f.JwtClaim);
            }

            mappings = mappings.ConcatOnce(Constants.DefaultClaimMappings.ChangeableMappings.Select(cm => new ClaimMap { JwtClaim = cm.JwtClaim, SamlClaim = cm.SamlClaim }), (f, s) => s.JwtClaim == f.JwtClaim);

            return mappings;
        }
    }
}
