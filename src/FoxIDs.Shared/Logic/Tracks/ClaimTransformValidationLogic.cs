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
        private readonly string[] replaceClaimOutJwtTypes = [JwtClaimTypes.Subject, JwtClaimTypes.SessionId, JwtClaimTypes.AuthTime, JwtClaimTypes.Acr, JwtClaimTypes.Amr, JwtClaimTypes.ExpirationTime, JwtClaimTypes.NotBefore, JwtClaimTypes.IssuedAt, JwtClaimTypes.Nonce, JwtClaimTypes.Azp, JwtClaimTypes.AtHash, JwtClaimTypes.CHash];
        private readonly string[] replaceClaimOutSamlTypes = [ClaimTypes.NameIdentifier, Saml2ClaimTypes.NameIdFormat, Saml2ClaimTypes.SessionIndex, ClaimTypes.Upn, ClaimTypes.AuthenticationInstant, ClaimTypes.AuthenticationMethod];

        public ClaimTransformValidationLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

        public void ValidateAndPrepareClaimTransforms<TClaimTransform>(IEnumerable<TClaimTransform> claimTransforms) where TClaimTransform : ClaimTransform
        {
            if (claimTransforms != null)
            {
                HandleObsoleteActions(claimTransforms);

                var addActionClaimTransform = claimTransforms.Where(ct => ct.Action == ClaimTransformActions.Add);

                foreach (var claimTransform in addActionClaimTransform)
                {
                    if (claimTransform.Type != ClaimTransformTypes.ExternalClaims)
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

        [Obsolete("backwards compatibility to support spelling error, remove method when 'ClaimTransformActions.AddIfNotObsolete' and 'ClaimTransformActions.ReplaceIfNotObsolete' is removed.")]
        private static void HandleObsoleteActions<TClaimTransform>(IEnumerable<TClaimTransform> claimTransforms) where TClaimTransform : ClaimTransform
        {
            foreach (var ct in claimTransforms)
            {
                HandleObsoleteActions(ct);
            }
        }

        [Obsolete("backwards compatibility to support spelling error, remove method when 'ClaimTransformActions.AddIfNotObsolete' and 'ClaimTransformActions.ReplaceIfNotObsolete' is removed.")]
        public static void HandleObsoleteActions(ClaimTransform ct)
        {
            if (ct.Action == ClaimTransformActions.AddIfNotObsolete)
            {
                ct.Action = ClaimTransformActions.AddIfNot;
            }
            else if (ct.Action == ClaimTransformActions.ReplaceIfNotObsolete)
            {
                ct.Action = ClaimTransformActions.ReplaceIfNot;
            }
        }
    }
}
