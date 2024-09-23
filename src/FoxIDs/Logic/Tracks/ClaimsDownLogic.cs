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
using FoxIDs.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Logic
{
    public class ClaimsDownLogic : LogicSequenceBase 
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IHttpContextAccessor httpContextAccessor;

        public ClaimsDownLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
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

        public async Task<List<Claim>> FromJwtToSamlClaimsAsync(IEnumerable<Claim> jwtClaims)
        {
            try
            {
                var mappings = GetMappings(RouteBinding, false);
                var newMappings = RouteBinding.AutoMapSamlClaims ? new List<ClaimMap>() : null;

                var samlClaims = new List<Claim>();

                FromJwtAuthTimeToSaml(samlClaims, jwtClaims);
                FromJwtAmrToSaml(samlClaims, jwtClaims);

                foreach (var jwtClaim in jwtClaims.Where(c => c.Type != JwtClaimTypes.AuthTime))
                {
                    var claimMaps = mappings.Where(m => m.JwtClaim.Equals(jwtClaim.Type, StringComparison.InvariantCultureIgnoreCase));
                    if (claimMaps?.Count() > 0)
                    {
                        foreach (var claimMap in claimMaps)
                        {
                            samlClaims.Add(new Claim(claimMap.SamlClaim, jwtClaim.Value, jwtClaim.ValueType, jwtClaim.Issuer, jwtClaim.OriginalIssuer));
                        }
                    }
                    else
                    {
                        if (RouteBinding.AutoMapSamlClaims)
                        {
                            samlClaims.Add(new Claim(AddNewJwtBasedMappingReturnSaml(mappings, newMappings, jwtClaim.Type), jwtClaim.Value, jwtClaim.ValueType, jwtClaim.Issuer, jwtClaim.OriginalIssuer));
                        }
                        else
                        {
                            samlClaims.Add(jwtClaim);
                        }
                    }
                }
                if (!samlClaims.Where(c => c.Type == Saml2ClaimTypes.NameIdFormat).Any())
                {
                    samlClaims.AddClaim(Saml2ClaimTypes.NameIdFormat, NameIdentifierFormats.Persistent.OriginalString);
                }

                await SaveNewMappingsAsync(newMappings);

                return samlClaims;
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

        public async Task<List<Claim>> FromSamlToJwtClaimsAsync(IEnumerable<Claim> samlClaims)
        {
            try
            {
                var mappings = GetMappings(RouteBinding, true);
                var newMappings = RouteBinding.AutoMapSamlClaims ? new List<ClaimMap>() : null;

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
                        if (RouteBinding.AutoMapSamlClaims)
                        {
                            jwtClaims.Add(new Claim(AddNewSamlBasedMappingReturnJwt(mappings, newMappings, samlClaim.Type), samlClaim.Value, samlClaim.ValueType, samlClaim.Issuer, samlClaim.OriginalIssuer));
                        }
                        else
                        {
                            var jwtClaim = new Claim(
                                samlClaim.Type?.Length > Constants.Models.Claim.JwtTypeLength ? samlClaim.Type.Substring(0, Constants.Models.Claim.JwtTypeLength) : samlClaim.Type,
                                samlClaim.Value, samlClaim.ValueType, samlClaim.Issuer, samlClaim.OriginalIssuer);

                            jwtClaims.Add(jwtClaim);
                        }
                    }
                }

                await SaveNewMappingsAsync(newMappings);

                return TruncateJwtClaimValues(jwtClaims);

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to map SAML claims to JWT claims.");
                throw;
            }
        }

        public List<string> FromSamlToJwtInfoClaimType(string samlClaimType)
        {
            try
            {
                var mappings = GetMappings(RouteBinding, true);

                var jwtClaimTypes = new List<string>();
                var claimMaps = mappings.Where(m => m.SamlClaim.Equals(samlClaimType, StringComparison.InvariantCultureIgnoreCase));
                if (claimMaps?.Count() > 0)
                {
                    foreach (var claimMap in claimMaps)
                    {
                        jwtClaimTypes.Add(claimMap.JwtClaim);
                    }
                }
                else if (!MappedClaimType(samlClaimType))
                {
                    jwtClaimTypes.Add(samlClaimType);
                }
                return jwtClaimTypes;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to map SAML claims to JWT claim types.");
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
                if (claim.Value?.Length > Constants.Models.Claim.ProcessValueLength)
                {
                    truncateClaims.AddClaim(claim.Type, claim.Value.Substring(0, Constants.Models.Claim.ProcessValueLength), claim.ValueType, claim.Issuer);
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

        private List<ClaimMap> GetMappings(RouteBinding RouteBinding, bool toJwtClaims)
        {
            var mappings = Constants.DefaultClaimMappings.LockedMappings.Select(cm => new ClaimMap { JwtClaim = cm.JwtClaim, SamlClaim = cm.SamlClaim }).ToList();

            if (RouteBinding.ClaimMappings != null && RouteBinding.ClaimMappings?.Count() > 0)
            {
                var useClaimMappings = new List<ClaimMap>();
                foreach (var claimMapping in RouteBinding.ClaimMappings)
                {
                    if (!mappings.Where(m => toJwtClaims ? m.SamlClaim == claimMapping.SamlClaim : m.JwtClaim == claimMapping.JwtClaim).Any())
                    {
                        useClaimMappings.Add(claimMapping);
                    }
                }
                mappings.AddRange(useClaimMappings);
            }

            mappings = mappings.ConcatOnce(Constants.DefaultClaimMappings.ChangeableMappings.Select(cm => new ClaimMap { JwtClaim = cm.JwtClaim, SamlClaim = cm.SamlClaim }), (f, s) => toJwtClaims ? s.SamlClaim == f.SamlClaim : s.JwtClaim == f.JwtClaim).ToList();
            return mappings;
        }


        private string AddNewJwtBasedMappingReturnSaml(List<ClaimMap> mappings, List<ClaimMap> newMappings, string jwtClaim)
        {
            var claimMap = new ClaimMap
            {
                JwtClaim = jwtClaim.ToLower(),
                SamlClaim = $"{Constants.SamlAutoMapClaimTypes.Namespace}{jwtClaim.Replace("_", "")}"
            };
            mappings.Add(claimMap);
            newMappings.Add(claimMap);

            return claimMap.SamlClaim;
        }

        private string AddNewSamlBasedMappingReturnJwt(List<ClaimMap> mappings, List<ClaimMap> newMappings, string samlClaim)
        {
            string jwtClaim = null;
            var claimSplit = samlClaim.Split('/');
            if (claimSplit.Length > 0)
            {
                var lastClaimSplit = claimSplit[claimSplit.Length - 1];
                if (lastClaimSplit.IsNullOrWhiteSpace() && claimSplit.Length > 1)
                {
                    lastClaimSplit = claimSplit[claimSplit.Length - 2];
                }

                var jwtClaimByTypeMap = Constants.SamlAutoMapClaimTypes.SamlToJwtTypeMappings.Where(c => c.Key.Equals(lastClaimSplit, StringComparison.InvariantCultureIgnoreCase)).Select(c => c.Value).FirstOrDefault();
                if (jwtClaimByTypeMap != null)
                {
                    jwtClaim = jwtClaimByTypeMap;
                }
                else
                {
                    jwtClaim = AddUnderscoreByUpperCase(lastClaimSplit);
                }
            }

            if (jwtClaim.IsNullOrEmpty())
            {
                jwtClaim = samlClaim.ToLower();
            }

            jwtClaim = jwtClaim.Length > Constants.Models.Claim.JwtTypeLength ? jwtClaim.Substring(0, Constants.Models.Claim.JwtTypeLength) : jwtClaim;

            var claimMap = new ClaimMap
            {
                JwtClaim = jwtClaim,
                SamlClaim = samlClaim,
            };
            mappings.Add(claimMap);
            newMappings.Add(claimMap);

            return jwtClaim;
        }

        private static string AddUnderscoreByUpperCase(string lastClaimSplit)
        {
            var claimShars = new List<char>();
            var firstCharacterOrlastUpperCase = true;
            for (int i = 0; i < lastClaimSplit.Length; i++)
            {
                var cItem = lastClaimSplit[i];
                if (char.IsUpper(cItem))
                {
                    if (!firstCharacterOrlastUpperCase)
                    {
                        claimShars.Add('_');
                        firstCharacterOrlastUpperCase = true;
                    }
                }
                else
                {
                    firstCharacterOrlastUpperCase = false;
                }
                claimShars.Add(cItem);
            }

            return string.Concat(claimShars).TrimStart().ToLower();
        }

        private async Task SaveNewMappingsAsync(List<ClaimMap> newMappings)
        {
            if (RouteBinding.AutoMapSamlClaims && newMappings?.Count() > 0)
            {
                var tenantDataRepository = httpContextAccessor.HttpContext.RequestServices.GetService<ITenantDataRepository>();
                var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mTrack = await tenantDataRepository.GetTrackByNameAsync(trackIdKey);

                if (mTrack.ClaimMappings == null)
                {
                    mTrack.ClaimMappings = new List<ClaimMap>();
                }
                foreach (var claimMap in newMappings)
                {
                    mTrack.ClaimMappings.Add(claimMap);
                }

                await tenantDataRepository.UpdateAsync(mTrack);
                RouteBinding.ClaimMappings = mTrack.ClaimMappings;
            }
        }
    }
}
