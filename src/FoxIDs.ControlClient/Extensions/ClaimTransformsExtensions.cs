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
                        if (claimTransform.Task == ClaimTransformTasks.QueryInternalUser || claimTransform.Task == ClaimTransformTasks.QueryExternalUser)
                        {
                            newClaimTransforms.Add(claimTransform.Map<OAuthClaimTransformClaimInClaimsOutViewModel>(afterMap =>
                            {
                                afterMap.ClaimIn = claimTransform.ClaimsIn?.FirstOrDefault();
                                if (afterMap.ClaimsOut == null || afterMap.ClaimsOut.Count == 0)
                                {
                                    afterMap.ClaimsOut = new List<string> { "*" };
                                }
                            }));
                        }
                        else 
                        {
                            newClaimTransforms.Add(claimTransform.Map<OAuthClaimTransformClaimInClaimOutViewModel>(afterMap =>
                            {
                                afterMap.ClaimIn = claimTransform.ClaimsIn?.FirstOrDefault();
                                afterMap.ClaimOut = claimTransform.ClaimsOut?.FirstOrDefault();
                            }));
                        }
                        break;
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
                        if (claimTransform.Task == ClaimTransformTasks.QueryInternalUser || claimTransform.Task == ClaimTransformTasks.QueryExternalUser)
                        {
                            newClaimTransforms.Add(claimTransform.Map<SamlClaimTransformClaimInClaimsOutViewModel>(afterMap =>
                            {
                                afterMap.ClaimIn = claimTransform.ClaimsIn?.FirstOrDefault();
                                if ((afterMap.Task == ClaimTransformTasks.QueryInternalUser || afterMap.Task == ClaimTransformTasks.QueryExternalUser) && (afterMap.ClaimsOut == null || afterMap.ClaimsOut.Count == 0))
                                {
                                    afterMap.ClaimsOut = new List<string> { "*" };
                                }
                            }));
                        }
                        else
                        {
                            newClaimTransforms.Add(claimTransform.Map<SamlClaimTransformClaimInClaimOutViewModel>(afterMap =>
                            {
                                afterMap.ClaimIn = claimTransform.ClaimsIn?.FirstOrDefault();
                                afterMap.ClaimOut = claimTransform.ClaimsOut?.FirstOrDefault();
                            }));
                        }
                        break;
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
                    if (claimTransform is OAuthClaimTransformClaimInClaimOutViewModel claimTransformClaimInClaimOut)
                    {
                        claimTransform.ClaimsIn = claimTransformClaimInClaimOut.ClaimIn.IsNullOrWhiteSpace() ? null : [claimTransformClaimInClaimOut.ClaimIn];
                        claimTransform.ClaimsOut = claimTransformClaimInClaimOut.ClaimOut.IsNullOrWhiteSpace() ? null : [claimTransformClaimInClaimOut.ClaimOut];
                    }
                    else if (claimTransform is OAuthClaimTransformClaimInClaimsOutViewModel claimTransformClaimInClaimsOut)
                    {
                        claimTransform.ClaimsIn = claimTransformClaimInClaimsOut.ClaimIn.IsNullOrWhiteSpace() ? null : [claimTransformClaimInClaimsOut.ClaimIn];
                    }
                    else if (claimTransform is OAuthClaimTransformClaimsInClaimOutViewModel claimTransformClaimsInClaimOut)
                    {
                        claimTransform.ClaimsOut = claimTransformClaimsInClaimOut.ClaimOut.IsNullOrWhiteSpace() ? null : [claimTransformClaimsInClaimOut.ClaimOut];
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
                    if (claimTransform is SamlClaimTransformClaimInClaimOutViewModel claimTransformClaimInClaimOut)
                    {
                        claimTransform.ClaimsIn = claimTransformClaimInClaimOut.ClaimIn.IsNullOrWhiteSpace() ? null : [claimTransformClaimInClaimOut.ClaimIn];
                        claimTransform.ClaimsOut = claimTransformClaimInClaimOut.ClaimOut.IsNullOrWhiteSpace() ? null : [claimTransformClaimInClaimOut.ClaimOut];
                    }
                    else if (claimTransform is SamlClaimTransformClaimInClaimsOutViewModel claimTransformClaimInClaimsOut)
                    {
                        claimTransform.ClaimsIn = claimTransformClaimInClaimsOut.ClaimIn.IsNullOrWhiteSpace() ? null : [claimTransformClaimInClaimsOut.ClaimIn];
                    }
                    else if (claimTransform is SamlClaimTransformClaimsInClaimOutViewModel claimTransformClaimsInClaimOut)
                    {
                        claimTransform.ClaimsOut = claimTransformClaimsInClaimOut.ClaimOut.IsNullOrWhiteSpace() ? null : [claimTransformClaimsInClaimOut.ClaimOut];
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
