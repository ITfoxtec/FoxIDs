using FoxIDs.Models.Api;

namespace FoxIDs.Client
{
    public static class LinkExternalUserExtensions
    {
        public static LinkExternalUser MapLinkExternalUserAfterMap(this LinkExternalUser linkExternalUser)
        {
            if (string.IsNullOrWhiteSpace(linkExternalUser?.LinkClaimType) && !(linkExternalUser?.AutoCreateUser == true || linkExternalUser?.RequireUser == true))
            {
                linkExternalUser = null;
            }

            if (linkExternalUser != null)
            {
                linkExternalUser.Elements.MapDynamicElementsAfterMap();
                linkExternalUser.ClaimTransforms.MapOAuthClaimTransformsAfterMap();
            }

            return linkExternalUser;
        }
    }
}
