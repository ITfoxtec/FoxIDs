using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System.Collections.Generic;

namespace FoxIDs.Client
{
    public static class ExtendedUiExtensions
    {
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
                }
            }
            return extendedUis;
        }
    }
}
