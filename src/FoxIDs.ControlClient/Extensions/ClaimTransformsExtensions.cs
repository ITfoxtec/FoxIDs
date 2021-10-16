using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Models.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Client
{
    public static class ClaimTransformsExtensions
    {
        public static List<OAuthClaimTransformViewModel> MapClaimTransforms(this List<OAuthClaimTransformViewModel> claimTransforms)
        {
            var newClaimTransforms = new List<OAuthClaimTransformViewModel>();
            foreach (var claimTransform in claimTransforms)
            {
                switch (claimTransform.Type)
                {
                    case ClaimTransformTypes.MatchClaim:
                    case ClaimTransformTypes.Match:
                    case ClaimTransformTypes.RegexMatch:
                    case ClaimTransformTypes.Map:
                    case ClaimTransformTypes.RegexMap:
                        newClaimTransforms.Add(new OAuthClaimTransformClaimInViewModel
                        {
                            Type = claimTransform.Type,
                            Order = claimTransform.Order,
                            ClaimsIn = claimTransform.ClaimsIn,
                            ClaimIn = claimTransform.ClaimsIn?.First(),
                            ClaimOut = claimTransform.ClaimOut,
                            Action = claimTransform.Action,
                            Transformation = claimTransform.Transformation,
                            TransformationExtension = claimTransform.TransformationExtension
                        });
                        break;
                    case ClaimTransformTypes.Constant:
                    case ClaimTransformTypes.Concatenate:
                        newClaimTransforms.Add(claimTransform);
                        break;
                    default:
                        throw new NotSupportedException("claim transform type not supported.");
                }
            }

            return newClaimTransforms;
        }

        public static List<SamlClaimTransformViewModel> MapClaimTransforms(this List<SamlClaimTransformViewModel> claimTransforms)
        {
            var newClaimTransforms = new List<SamlClaimTransformViewModel>();
            foreach (var claimTransform in claimTransforms)
            {
                switch (claimTransform.Type)
                {
                    case ClaimTransformTypes.MatchClaim:
                    case ClaimTransformTypes.Match:
                    case ClaimTransformTypes.RegexMatch:
                    case ClaimTransformTypes.Map:
                    case ClaimTransformTypes.RegexMap:
                        newClaimTransforms.Add(new SamlClaimTransformClaimInViewModel
                        {
                            Type = claimTransform.Type,
                            Order = claimTransform.Order,
                            ClaimsIn = claimTransform.ClaimsIn,
                            ClaimIn = claimTransform.ClaimsIn?.First(),
                            ClaimOut = claimTransform.ClaimOut,
                            Action = claimTransform.Action,
                            Transformation = claimTransform.Transformation,
                            TransformationExtension = claimTransform.TransformationExtension
                        });
                        break;
                    case ClaimTransformTypes.Constant:
                    case ClaimTransformTypes.Concatenate:
                        newClaimTransforms.Add(claimTransform);
                        break;
                    default:
                        throw new NotSupportedException("claim transform type not supported.");
                }
            }

            return newClaimTransforms;
        }
    }
}
