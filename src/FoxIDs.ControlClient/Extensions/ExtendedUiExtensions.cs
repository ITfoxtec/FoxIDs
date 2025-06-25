using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System.Collections.Generic;

namespace FoxIDs.Client
{
    public static class ExtendedUiExtensions
    {
        public static List<ExtendedUi> MapExtendedUis(this List<ExtendedUi> extendedUis)
        {
            if (extendedUis?.Count > 0)
            {
                foreach (var extendedUi in extendedUis)
                {
                    if (extendedUi.Secret != null)
                    {
                        extendedUi.Secret = extendedUi.SecretLoaded = extendedUi.Secret.Length == 3 ? $"{extendedUi.Secret}..." : extendedUi.Secret;
                    }
                }
            }
            return extendedUis;
        }

        public static List<ExtendedUiViewModel> MapExtendedUis(this List<ExtendedUiViewModel> extendedUis)
        {
            if (extendedUis?.Count > 0)
            {
                foreach (var extendedUi in extendedUis)
                {
                    if (extendedUi.ClaimTransforms?.Count > 0)
                    {
                        extendedUi.ClaimTransforms = extendedUi.ClaimTransforms.MapOAuthClaimTransforms();
                    }
                }
            }
            return extendedUis;
        }

        public static List<ExtendedUiViewModel> MapExtendedUisBeforeMap(this List<ExtendedUiViewModel> extendedUis)
        {
            foreach (var extendedUi in extendedUis)
            {
                extendedUi.ClaimTransforms.MapOAuthClaimTransformsBeforeMap();
            }
            return extendedUis;
        }


        public static List<ExtendedUi> MapExtendedUisAfterMap(this List<ExtendedUi> extendedUis)
        {
            foreach (var extendedUi in extendedUis)
            {
                extendedUi.Elements.MapDynamicElementsAfterMap();
                extendedUi.ClaimTransforms.MapOAuthClaimTransformsAfterMap();
                if (extendedUi.ApiUrl.IsNullOrEmpty())
                {
                    extendedUi.ExternalConnectType = null;
                }
                else
                {
                    extendedUi.ExternalConnectType = ExternalConnectTypes.Api;
                    if (extendedUi.Secret == extendedUi.SecretLoaded)
                    {
                        extendedUi.Secret = null;
                    }
                }
            }
            return extendedUis;
        }
    }
}
