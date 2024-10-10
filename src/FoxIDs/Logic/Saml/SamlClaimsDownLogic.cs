using FoxIDs.Models;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Claims;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens.Saml2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace FoxIDs.Logic
{
    public class SamlClaimsDownLogic : LogicSequenceBase
    {
        public SamlClaimsDownLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

        public string GetSessionIndex(IEnumerable<Claim> claims)
        {
            return claims.FindFirstOrDefaultValue(c => c.Type == Saml2ClaimTypes.SessionIndex); 
        }

        public Saml2NameIdentifier GetNameId(IEnumerable<Claim> claims, string overwriteNameIdFormat = null)
        {
            var nameIdValue = string.Empty;

            if (NameIdentifierFormats.Email.OriginalString.Equals(overwriteNameIdFormat, StringComparison.OrdinalIgnoreCase))
            {
                nameIdValue = claims.FindFirstOrDefaultValue(c => c.Type == ClaimTypes.Email);
            }

            if (nameIdValue.IsNullOrEmpty())
            {
                nameIdValue = claims.FindFirstOrDefaultValue(c => c.Type == ClaimTypes.NameIdentifier);
            }
            else if (nameIdValue.IsNullOrEmpty())
            {
                nameIdValue = claims.FindFirstOrDefaultValue(c => c.Type == ClaimTypes.Upn);
            }
            else if (nameIdValue.IsNullOrEmpty())
            {
                nameIdValue = claims.FindFirstOrDefaultValue(c => c.Type == ClaimTypes.Email);
            }
            else if (nameIdValue.IsNullOrEmpty())
            {
                nameIdValue = claims.FindFirstOrDefaultValue(c => c.Type == ClaimTypes.Name);
            }

            if (nameIdValue.IsNullOrEmpty())
            {
                return null;
            }
            else
            {
                var nameIdFormat = GetNameIdFormat(claims, overwriteNameIdFormat);
                if (nameIdFormat != null)
                {
                    return new Saml2NameIdentifier(nameIdValue, nameIdFormat);
                }
                else
                {
                    return new Saml2NameIdentifier(nameIdValue);
                }
            }
        }

        private Uri GetNameIdFormat(IEnumerable<Claim> claims, string overwriteNameIdFormat)
        {
            if (!overwriteNameIdFormat.IsNullOrWhiteSpace())
            {
                return new Uri(overwriteNameIdFormat);
            }
            else
            {
                var nameIdFormat = claims.FindFirstOrDefaultValue(c => c.Type == Saml2ClaimTypes.NameIdFormat);
                if (!nameIdFormat.IsNullOrWhiteSpace())
                {
                    return new Uri(nameIdFormat);
                }
                else
                {
                    return null;
                }
            }
        }

        public IEnumerable<Claim> GetSubjectClaims(SamlDownParty party, IEnumerable<Claim> claims)
        {
            var acceptAllClaims = party.Claims?.Where(c => c == "*")?.Count() > 0;
            if (!acceptAllClaims)
            {
                var acceptedClaims = Constants.DefaultClaims.SamlClaims.ConcatOnce(party.Claims);
                claims = claims.Where(c => acceptedClaims.Any(ic => ic == c.Type));
            }
            return claims;
        }
    }
}
