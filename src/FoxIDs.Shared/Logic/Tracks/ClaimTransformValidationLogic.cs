using FoxIDs.Models;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Claims;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace FoxIDs.Logic
{
    public class ClaimTransformValidationLogic : LogicBase
    {
        private readonly string[] replaceClaimOutJwtTypes = new[] { JwtClaimTypes.Subject, JwtClaimTypes.SessionId, JwtClaimTypes.AuthTime, JwtClaimTypes.Acr, JwtClaimTypes.Amr, JwtClaimTypes.ExpirationTime, JwtClaimTypes.NotBefore, JwtClaimTypes.IssuedAt, JwtClaimTypes.Nonce, JwtClaimTypes.Azp, JwtClaimTypes.AtHash, JwtClaimTypes.CHash };
        private readonly string[] replaceClaimOutSamlTypes = new[] { ClaimTypes.NameIdentifier, Saml2ClaimTypes.NameIdFormat, Saml2ClaimTypes.SessionIndex, ClaimTypes.Upn, ClaimTypes.AuthenticationInstant, ClaimTypes.AuthenticationMethod };

        public ClaimTransformValidationLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

        public void ValidateAndPrepareClaimTransforms<TClaimTransform>(IEnumerable<TClaimTransform> claimTransforms) where TClaimTransform : ClaimTransform
        {
            var addActionClaimTransform = claimTransforms.Where(ct => ct.Action == ClaimTransformActions.Add);

            foreach (var claimTransform in addActionClaimTransform)
            {
                if (claimTransform is OAuthClaimTransform)
                {
                    if (replaceClaimOutJwtTypes.Any(rc => claimTransform.ClaimOut.Equals(rc, StringComparison.OrdinalIgnoreCase)))
                    {
                        claimTransform.Action = ClaimTransformActions.Replace;
                    }
                }
                else if (claimTransform is SamlClaimTransform)
                {
                    if (replaceClaimOutSamlTypes.Any(rc => claimTransform.ClaimOut.Equals(rc, StringComparison.OrdinalIgnoreCase)))
                    {
                        claimTransform.Action = ClaimTransformActions.Replace;
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }

            }
        }
    }
}
