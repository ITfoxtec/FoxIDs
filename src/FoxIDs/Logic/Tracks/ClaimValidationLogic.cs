﻿using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace FoxIDs.Logic
{
    public class ClaimValidationLogic : LogicSequenceBase
    {
        public ClaimValidationLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

        public List<Claim> ValidateUpPartyClaims(List<string> upPartyClaims, List<Claim> claims)
        {
            var acceptAllClaims = upPartyClaims?.Where(c => c == "*")?.Count() > 0;
            if (acceptAllClaims)
            {
                claims = claims.Where(c => !Constants.DefaultClaims.ExcludeJwtTokenUpParty.Any(ic => ic == c.Type)).ToList();
            }
            else
            {
                var acceptedClaims = Constants.DefaultClaims.JwtTokenUpParty.ConcatOnce(upPartyClaims).Where(c => !Constants.DefaultClaims.ExcludeJwtTokenUpParty.Contains(c));
                claims = claims.Where(c => acceptedClaims.Any(ic => ic == c.Type)).ToList();
            }
            var totalValueLenght = 0;
            foreach (var claim in claims)
            {
                if (claim.Type?.Length > Constants.Models.Claim.JwtTypeLength)
                {
                    throw new OAuthRequestException($"Claim '{claim.Type.Substring(0, Constants.Models.Claim.JwtTypeLength)}' is too long, maximum length of '{Constants.Models.Claim.JwtTypeLength}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                }

                if (claim.Value?.Length > Constants.Models.Claim.ProcessValueLength)
                {
                    throw new OAuthRequestException($"Claim '{claim.Type}' value is too long, maximum length of '{Constants.Models.Claim.ProcessValueLength}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                }
                if (claim.Value?.Length > 0)
                {
                    totalValueLenght += claim.Value.Length;
                }
            }
            if (totalValueLenght > Constants.Models.Claim.ProcessValueLength)
            {
                throw new OAuthRequestException($"The total length of all claim values combined is too long, maximum length of '{Constants.Models.Claim.ProcessValueLength}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
            }
            return claims;
        }
    }
}
