using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Client
{
    public static class ClaimTransformsExtensions
    {
        public static List<ClaimTransformViewModel> MapOAuthClaimTransforms(this List<ClaimTransformViewModel> claimTransforms)
        {
            var newClaimTransforms = new List<ClaimTransformViewModel>();
            foreach (var claimTransform in claimTransforms)
            {
                switch (claimTransform.Type)
                {
                    case ClaimTransformTypes.MatchClaim:
                    case ClaimTransformTypes.Match:
                    case ClaimTransformTypes.RegexMatch:
                    case ClaimTransformTypes.Map:
                    case ClaimTransformTypes.RegexMap:
                    case ClaimTransformTypes.DkPrivilege:
                        var oauthClaimTransformClaimIn = claimTransform.Map<OAuthClaimTransformClaimInViewModel>();
                        oauthClaimTransformClaimIn.ClaimIn = claimTransform.ClaimsIn?.First();
                        newClaimTransforms.Add(oauthClaimTransformClaimIn);
                        break;
                    case ClaimTransformTypes.Constant:
                        newClaimTransforms.Add(claimTransform.Map<OAuthClaimTransformClaimInViewModel>());
                        break;
                    case ClaimTransformTypes.Concatenate:
                        newClaimTransforms.Add(claimTransform.Map<OAuthClaimTransformClaimsInViewModel>());
                        break;
                    case ClaimTransformTypes.ExternalClaims:
                        var oauthClaimTransformClaimsIn = claimTransform.Map<OAuthClaimTransformClaimsInViewModel>();
                        oauthClaimTransformClaimsIn.Secret = oauthClaimTransformClaimsIn.SecretLoaded = claimTransform.Secret.Length == 3 ? $"{claimTransform.Secret}..." : claimTransform.Secret;
                        newClaimTransforms.Add(oauthClaimTransformClaimsIn);
                        break;
                    default:
                        throw new NotSupportedException("claim transform type not supported.");
                }
            }

            return newClaimTransforms;
        }

        public static List<ClaimTransformViewModel> MapSamlClaimTransforms(this List<ClaimTransformViewModel> claimTransforms)
        {
            var newClaimTransforms = new List<ClaimTransformViewModel>();
            foreach (var claimTransform in claimTransforms)
            {
                switch (claimTransform.Type)
                {
                    case ClaimTransformTypes.MatchClaim:
                    case ClaimTransformTypes.Match:
                    case ClaimTransformTypes.RegexMatch:
                    case ClaimTransformTypes.Map:
                    case ClaimTransformTypes.RegexMap:
                    case ClaimTransformTypes.DkPrivilege:
                        var samlClaimTransformClaimIn = claimTransform.Map<SamlClaimTransformClaimInViewModel>();
                        samlClaimTransformClaimIn.ClaimIn = claimTransform.ClaimsIn?.First();
                        newClaimTransforms.Add(samlClaimTransformClaimIn);
                        break;
                    case ClaimTransformTypes.Constant:
                        newClaimTransforms.Add(claimTransform.Map<SamlClaimTransformClaimInViewModel>());
                        break;
                    case ClaimTransformTypes.Concatenate:
                        newClaimTransforms.Add(claimTransform.Map<SamlClaimTransformClaimsInViewModel>());
                        break;
                    case ClaimTransformTypes.ExternalClaims:
                        var samlClaimTransformClaimsIn = claimTransform.Map<SamlClaimTransformClaimsInViewModel>();
                        samlClaimTransformClaimsIn.Secret = samlClaimTransformClaimsIn.SecretLoaded = claimTransform.Secret.Length == 3 ? $"{claimTransform.Secret}..." : claimTransform.Secret;
                        newClaimTransforms.Add(samlClaimTransformClaimsIn);
                        break;
                    default:
                        throw new NotSupportedException("claim transform type not supported.");
                }
            }

            return newClaimTransforms;
        }

        public static List<ClaimTransformViewModel> MapOAuthClaimTransformsBeforeMap(this List<ClaimTransformViewModel> claimTransforms)
        {
            if (claimTransforms?.Count() > 0)
            {
                foreach (var claimTransform in claimTransforms)
                {
                    if (claimTransform is OAuthClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                    {
                        claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                    }

                    if (claimTransform.Type == ClaimTransformTypes.ExternalClaims && claimTransform.ExternalConnectType == ExternalConnectTypes.Api && claimTransform.Secret == claimTransform.SecretLoaded)
                    {
                        claimTransform.Secret = null;
                    }
                }
            }

            return claimTransforms;
        }

        public static List<ClaimTransformViewModel> MapSamlClaimTransformsBeforeMap(this List<ClaimTransformViewModel> claimTransforms)
        {
            if (claimTransforms?.Count() > 0)
            {
                foreach (var claimTransform in claimTransforms)
                {
                    if (claimTransform is SamlClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                    {
                        claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                    }

                    if (claimTransform.Type == ClaimTransformTypes.ExternalClaims && claimTransform.ExternalConnectType == ExternalConnectTypes.Api && claimTransform.Secret == claimTransform.SecretLoaded)
                    {
                        claimTransform.Secret = null;
                    }
                }
            }

            return claimTransforms;
        }

        public static List<OAuthClaimTransform> MapOAuthClaimTransformsAfterMap(this List<OAuthClaimTransform> claimTransforms)
        {
            if (claimTransforms?.Count() > 0)
            {
                int order = 1;
                foreach (var claimTransform in claimTransforms)
                {
                    claimTransform.Order = order++;
                }
            }

            return claimTransforms;
        }

        public static List<SamlClaimTransform> MapSamlClaimTransformsAfterMap(this List<SamlClaimTransform> claimTransforms)
        {
            if (claimTransforms?.Count() > 0)
            {
                int order = 1;
                foreach (var claimTransform in claimTransforms)
                {
                    claimTransform.Order = order++;
                }
            }

            return claimTransforms;
        }
    }
}
