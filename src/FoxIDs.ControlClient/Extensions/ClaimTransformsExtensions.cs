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
                        newClaimTransforms.Add(claimTransform.Map<OAuthClaimTransformClaimInClaimOutViewModel>(afterMap =>
                        {
                            afterMap.ClaimIn = claimTransform.ClaimsIn?.FirstOrDefault();
                            afterMap.ClaimOut = claimTransform.ClaimsOut?.FirstOrDefault();
                        }));
                        break;
                    case ClaimTransformTypes.Constant:
                        newClaimTransforms.Add(claimTransform.Map<OAuthClaimTransformClaimInClaimOutViewModel>(afterMap =>
                        {
                            afterMap.ClaimOut = claimTransform.ClaimsOut?.FirstOrDefault();
                        }));
                        break;
                    case ClaimTransformTypes.Concatenate:
                        newClaimTransforms.Add(claimTransform.Map<OAuthClaimTransformClaimsInClaimOutViewModel>(afterMap =>
                        {
                            afterMap.ClaimOut = claimTransform.ClaimsOut?.FirstOrDefault();
                        }));
                        break;
                    case ClaimTransformTypes.ExternalClaims:
                        newClaimTransforms.Add(claimTransform.Map<OAuthClaimTransformClaimsInClaimOutViewModel>(afterMap =>
                        {
                            afterMap.ClaimOut = claimTransform.ClaimsOut?.FirstOrDefault();
                        }));
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
                        newClaimTransforms.Add(claimTransform.Map<SamlClaimTransformClaimInClaimOutViewModel>(afterMap =>
                        {
                            afterMap.ClaimIn = claimTransform.ClaimsIn?.FirstOrDefault();
                            afterMap.ClaimOut = claimTransform.ClaimsOut?.FirstOrDefault();
                        }));
                        break;
                    case ClaimTransformTypes.Constant:
                        newClaimTransforms.Add(claimTransform.Map<SamlClaimTransformClaimInClaimOutViewModel>(afterMap =>
                        {
                            afterMap.ClaimOut = claimTransform.ClaimsOut?.FirstOrDefault();
                        }));
                        break;
                    case ClaimTransformTypes.Concatenate:
                        newClaimTransforms.Add(claimTransform.Map<SamlClaimTransformClaimsInClaimOutViewModel>(afterMap =>
                        {
                            afterMap.ClaimOut = claimTransform.ClaimsOut?.FirstOrDefault();
                        }));
                        break;
                    case ClaimTransformTypes.ExternalClaims:
                        newClaimTransforms.Add(claimTransform.Map<SamlClaimTransformClaimsInClaimOutViewModel>(afterMap =>
                        {
                            afterMap.ClaimOut = claimTransform.ClaimsOut?.FirstOrDefault();
                        }));
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
                    if (claimTransform is OAuthClaimTransformClaimInClaimOutViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                    {
                        claimTransform.ClaimsIn = claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace() ? null : [claimTransformClaimIn.ClaimIn];
                        claimTransform.ClaimsOut = claimTransformClaimIn.ClaimOut.IsNullOrWhiteSpace() ? null : [claimTransformClaimIn.ClaimOut];
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
                    if (claimTransform is SamlClaimTransformClaimInClaimOutViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                    {
                        claimTransform.ClaimsIn = claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace() ? null : [claimTransformClaimIn.ClaimIn];
                        claimTransform.ClaimsOut = claimTransformClaimIn.ClaimOut.IsNullOrWhiteSpace() ? null : [claimTransformClaimIn.ClaimOut];
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
